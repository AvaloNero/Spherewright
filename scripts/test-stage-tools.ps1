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
    $script:actionId = ''; $script:failure = $Failure; $script:researchPayload = $null; $script:savePayload = $null; $script:researchBudget = @(); $script:recorded = $null
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
$arguments = @{SessionId='fixture-session';PlanetId=104;GameVersion='fixture-version';TechId=2104;AcceptedBefore=8;AuditWindowLimit=10;ValidateResearchPlan={
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
Assert-Stage ($result.auditWindowLimit -eq 10 -and $script:recorded.auditWindowLimit -eq 10) 'explicit ten-write limit is preserved in result and evidence'
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
foreach ($boundary in @(
    [pscustomobject]@{limit=20;before=17;after=19;frozen=$false},
    [pscustomobject]@{limit=20;before=18;after=20;frozen=$true},
    [pscustomobject]@{limit=50;before=47;after=49;frozen=$false},
    [pscustomobject]@{limit=50;before=48;after=50;frozen=$true}
)) {
    $windowArguments = @{}; foreach ($key in $arguments.Keys) { $windowArguments[$key] = $arguments[$key] }
    $windowArguments.AuditWindowLimit = $boundary.limit
    $windowArguments.AcceptedBefore = $boundary.before
    Reset-Stage
    $windowResult = Invoke-SpherewrightResearchAndSave @windowArguments
    Assert-Stage ($windowResult.acceptedDelta -eq 2 -and $windowResult.acceptedAfter -eq $boundary.after -and
        $windowResult.frozen -eq $boundary.frozen -and $windowResult.auditWindowLimit -eq $boundary.limit) "research/save exact $($boundary.limit)-write boundary from $($boundary.before)"
}
foreach ($limit in @(20,50)) {
    $windowArguments = @{}; foreach ($key in $arguments.Keys) { $windowArguments[$key] = $arguments[$key] }
    $windowArguments.AuditWindowLimit = $limit
    $windowArguments.AcceptedBefore = $limit - 1
    Reset-Stage
    $errorRecord = $null
    try { Invoke-SpherewrightResearchAndSave @windowArguments | Out-Null } catch { $errorRecord = $_ }
    Assert-Stage ($null -ne $errorRecord -and $script:methods.Count -eq 0) "research/save $limit-write window reserves both slots before any read"
}
$defaultResearchArguments = @{}; foreach ($key in $arguments.Keys) { $defaultResearchArguments[$key] = $arguments[$key] }
$defaultResearchArguments.Remove('AuditWindowLimit')
$defaultResearchArguments.AcceptedBefore = 18
Reset-Stage
$defaultResearchResult = Invoke-SpherewrightResearchAndSave @defaultResearchArguments
Assert-Stage ($defaultResearchResult.acceptedDelta -eq 2 -and $defaultResearchResult.acceptedAfter -eq 20 -and
    $defaultResearchResult.frozen -and $defaultResearchResult.auditWindowLimit -eq 20 -and $script:recorded.auditWindowLimit -eq 20) 'omitted research limit defaults to 20 and exactly fills 18 to 20'
$defaultResearchArguments.AcceptedBefore = 19
Reset-Stage
$defaultResearchBudgetError = $null
try { Invoke-SpherewrightResearchAndSave @defaultResearchArguments | Out-Null } catch { $defaultResearchBudgetError = $_ }
Assert-Stage ($null -ne $defaultResearchBudgetError -and $script:methods.Count -eq 0) 'default 20 research window rejects 19 before any request when two slots do not fit'
$invalidWindowArguments = @{}; foreach ($key in $arguments.Keys) { $invalidWindowArguments[$key] = $arguments[$key] }
$invalidWindowArguments.AuditWindowLimit = 11
Reset-Stage
$invalidWindowRejected = $false
try { Invoke-SpherewrightResearchAndSave @invalidWindowArguments | Out-Null } catch { $invalidWindowRejected = $true }
Assert-Stage ($invalidWindowRejected -and $script:methods.Count -eq 0) 'unsupported audit window limit is rejected before any request'
Reset-Stage
$readOnlyBudgetError = $null
$readOnlyArguments = @{ApprovedPlan=@{};ExpectedRevision=0;AcceptedBefore=11;AuditWindowLimit=10;MinimumDurableSequence=0;RecordEvidence={}}
try { Invoke-SpherewrightBeltSiteQualification @readOnlyArguments | Out-Null } catch { $readOnlyBudgetError = $_ }
Assert-Stage ($null -ne $readOnlyBudgetError -and $readOnlyBudgetError.Exception.Message -like '*insufficient slots*' -and
    $script:methods.Count -eq 0) 'read-only qualification rejects a counter above its window before validation or requests'
function gh { $global:LASTEXITCODE=0; '[{"databaseId":1,"headSha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","status":"completed","conclusion":"success","url":"https://example.invalid/ci"}]' }
$ciFixture=Get-SpherewrightCommitChecks -CommitSha ('a'*40)
Assert-Stage ($ciFixture.observed -and $ciFixture.runs[0].conclusion -ceq 'success' -and -not $ciFixture.signOff) 'exact SHA CI snapshot is not independent sign-off'
function gh { $global:LASTEXITCODE=0; '[]' }
$ciFixture=Get-SpherewrightCommitChecks -CommitSha ('a'*40)
Assert-Stage (-not $ciFixture.observed) 'absent workflow run stays unknown'

# Offline Git fixture: classification uses only the requested SHA's exact diff-tree.
$priorGitFunction = Get-Item -Path Function:\git -ErrorAction SilentlyContinue
$hadPriorGitFunction = $null -ne $priorGitFunction
$priorGitScriptBlock = if ($hadPriorGitFunction) { $priorGitFunction.ScriptBlock } else { $null }
$priorLastExitVariable = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
$hadPriorLastExitCode = $null -ne $priorLastExitVariable
$priorLastExitCode = if ($hadPriorLastExitCode) { $priorLastExitVariable.Value } else { $null }
try {
    $script:impactGitRows = @(); $script:impactGitExitCode = 0; $script:impactGitCallCount = 0; $script:impactGitArguments = @()
    function git {
        $script:impactGitCallCount++
        $script:impactGitArguments = @($args)
        $global:LASTEXITCODE = $script:impactGitExitCode
        foreach ($row in $script:impactGitRows) { Write-Output $row }
    }
    function Set-CommitImpactFixture([object[]]$Rows, [int]$ExitCode = 0) {
        $script:impactGitRows = @($Rows)
        $script:impactGitExitCode = $ExitCode
        $script:impactGitCallCount = 0
        $script:impactGitArguments = @()
    }
    $impactSha = 'a' * 40
    $impactRepo = 'C:\fixture\spherewright'
    $factDiff = @(
        ('A' + "`t" + 'docs/evidence/2026-10-08/stage-note.md'),
        ('M' + "`t" + 'docs/incidents/workflow-note.md'),
        ('M' + "`t" + 'docs/current-status.md'),
        ('A' + "`t" + 'docs/gameplay-timeline.md')
    )
    Set-CommitImpactFixture $factDiff
    $impact = Get-SpherewrightCommitImpact -CommitSha $impactSha -RepositoryDirectory $impactRepo
    $expectedGitArgs = @('-C',$impactRepo,'diff-tree','--root','--no-commit-id','--name-status','--no-renames','-r',$impactSha)
    Assert-Stage ($impact.deliveryOnly -and -not $impact.executionRelevant -and -not $impact.signOff -and
        $impact.changedPaths.Count -eq 4) 'exact allowed fact-only A/M commit is delivery-only, not sign-off'
    Assert-Stage ($script:impactGitCallCount -eq 1 -and
        ($script:impactGitArguments -join '|') -ceq ($expectedGitArgs -join '|')) 'impact classification calls only the prescribed SHA diff-tree command'
    Set-CommitImpactFixture @($factDiff + @('M' + "`t" + 'scripts/changed.ps1'))
    $impact = Get-SpherewrightCommitImpact -CommitSha $impactSha -RepositoryDirectory $impactRepo
    Assert-Stage (-not $impact.deliveryOnly -and $impact.executionRelevant) 'mixed fact and source commit remains execution-relevant'
    Set-CommitImpactFixture @(
        'M' + "`t" + 'AGENTS.md',
        'M' + "`t" + 'docs/agent-playbook.md',
        'M' + "`t" + 'ROADMAP.md'
    )
    $impact = Get-SpherewrightCommitImpact -CommitSha $impactSha -RepositoryDirectory $impactRepo
    Assert-Stage (-not $impact.deliveryOnly -and $impact.executionRelevant) 'rules, playbook and roadmap documentation remain execution-relevant'
    Set-CommitImpactFixture @('D' + "`t" + 'docs/evidence/2026-10-08/deleted-note.md')
    $impact = Get-SpherewrightCommitImpact -CommitSha $impactSha -RepositoryDirectory $impactRepo
    Assert-Stage (-not $impact.deliveryOnly -and $impact.executionRelevant) 'deleting an otherwise allowed fact file remains execution-relevant'
    Set-CommitImpactFixture @()
    $impact = Get-SpherewrightCommitImpact -CommitSha $impactSha -RepositoryDirectory $impactRepo
    Assert-Stage (-not $impact.deliveryOnly -and $impact.executionRelevant -and $impact.changedPaths.Count -eq 0) 'empty merge diff remains execution-relevant'
    Set-CommitImpactFixture @('M')
    $ambiguousImpactRejected = $false
    try { Get-SpherewrightCommitImpact -CommitSha $impactSha -RepositoryDirectory $impactRepo | Out-Null } catch { $ambiguousImpactRejected = $true }
    Assert-Stage ($ambiguousImpactRejected) 'malformed Git status row fails closed'
    Set-CommitImpactFixture @('M' + "`t" + 'docs/current-status.md') 1
    $failedImpactRejected = $false
    try { Get-SpherewrightCommitImpact -CommitSha $impactSha -RepositoryDirectory $impactRepo | Out-Null } catch { $failedImpactRejected = $true }
    Assert-Stage ($failedImpactRejected) 'nonzero Git diff-tree exit fails closed'
    Assert-Stage ($global:LASTEXITCODE -eq 1) 'Git failure fixture sets a nonzero shell status before restoration'
} finally {
    if ($hadPriorGitFunction) {
        Set-Item -Path Function:\git -Value $priorGitScriptBlock -Force
    } else {
        Remove-Item -Path Function:\git -Force -ErrorAction SilentlyContinue
    }
    if ($hadPriorLastExitCode) {
        Set-Variable -Name LASTEXITCODE -Scope Global -Value $priorLastExitCode -Force
    } else {
        Remove-Variable -Name LASTEXITCODE -Scope Global -Force -ErrorAction SilentlyContinue
    }
}
$restoredGitFunction = Get-Item -Path Function:\git -ErrorAction SilentlyContinue
$gitMockRestored = if ($hadPriorGitFunction) {
    $null -ne $restoredGitFunction -and $restoredGitFunction.ScriptBlock.ToString() -ceq $priorGitScriptBlock.ToString()
} else {
    $null -eq $restoredGitFunction
}
$restoredLastExitVariable = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
$lastExitCodeRestored = if ($hadPriorLastExitCode) {
    $null -ne $restoredLastExitVariable -and $restoredLastExitVariable.Value -eq $priorLastExitCode
} else {
    $null -eq $restoredLastExitVariable
}
Assert-Stage ($gitMockRestored) 'Git fixture removes its mock or restores the prior function'
Assert-Stage ($lastExitCodeRestored) 'Git fixture restores the pre-test LASTEXITCODE exactly'

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

# Material transfer -> handcraft -> save fixtures use the real normal-action
# client above and replace only its existing offline transport.
$script:materialChecks = 0
function Assert-MaterialStage([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Material stage regression: $Name" }
    $script:materialChecks++
}
function Reset-MaterialStage([string]$Failure = '') {
    $script:methods = [Collections.Generic.List[string]]::new()
    $script:commitMethods = [Collections.Generic.List[string]]::new()
    $script:fixtureCommits = [Collections.Generic.List[object]]::new()
    $script:terminalActionIds = [Collections.Generic.List[string]]::new()
    $script:playerQueueCounts = [Collections.Generic.List[int]]::new()
    $script:materialFailure = $Failure
    $script:sessionReads = 0; $script:journalReads = 0; $script:playerReads = 0; $script:sourceReads = 0
    $script:revisionBeforeSave = 0; $script:savePayload = $null
    $script:materialError = $null; $script:recordedMaterialSummary = $null
    $script:craftReadbackCalls = 0; $script:gameCalls = 0; $script:hiddenPossibleActionId = ''
    $script:materialItemId = 1301; $script:materialCount = 1; $script:recipeId = 85; $script:craftCount = 1
}
function Invoke-SpherewrightBridgeRequest([string]$Method, [string]$SessionId, [hashtable]$Payload) {
    # This replacement is the local fixture transport; Invoke-SpherewrightNormalAction
    # remains the production implementation and every request is recorded here.
    $script:methods.Add($Method)
    if ($SessionId -cne 'fixture-session') { throw 'Unexpected material fixture session.' }
    $result = switch ($Method) {
        get_session_state {
            $script:sessionReads++
            $revisions = @(13,29,83,157,259)
            $revisionIndex = [int][Math]::Min($script:sessionReads - 1, $revisions.Count - 1)
            $revision = $revisions[$revisionIndex]
            if ($script:sessionReads -eq 3) { $script:revisionBeforeSave = $revision }
            [pscustomobject]@{
                sessionId=$SessionId;localPlanetId=104;gameVersion='fixture-version';gameLoaded=$true
                ownedBySpherewright=$true;accessRestricted=$false;writeHealth='healthy';writeBlockers=@()
                writesAllowed=$true;peacefulMode='confirmed_peaceful';gameTick=(80440000 + 100*$script:sessionReads)
                revision=$revision;ownedSaveState='saved'
                lastOwnedSaveGameTick=$(if($script:sessionReads -ge 4){80442879}else{80425709})
                restartResumeAvailable=$true
            }
        }
        get_gameplay_journal {
            $script:journalReads++
            $highest = if ($script:journalReads -eq 1) { 96 } else { 101 }
            $pending = ($script:materialFailure -ceq 'journal_pending' -and $script:journalReads -gt 1)
            [pscustomobject]@{
                sessionId=$SessionId
                entries=@(1..$highest | ForEach-Object { [pscustomobject]@{sequence=$_} })
                durableThroughSequence=$highest;persistencePending=$pending;persistenceError=$null
            }
        }
        get_player_state {
            $script:playerReads++
            $materialInventory = 0
            if ($script:playerReads -ge 2) { $materialInventory += $script:materialCount }
            if ($script:playerReads -ge 3) { $materialInventory -= $script:materialCount }
            $firstInputInventory = if ($script:playerReads -lt 3) { 1 } else { 0 }
            $craftedInventory = if ($script:playerReads -ge 3) { 1 } else { 0 }
            $queue = @()
            if ($script:materialFailure -ceq 'forge_queue' -and $script:playerReads -eq 1) {
                $queue = @([pscustomobject]@{recipeId=999;count=1})
            }
            if ($script:materialFailure -ceq 'craft_readback' -and $script:playerReads -eq 3) {
                $queue = @([pscustomobject]@{recipeId=$script:recipeId;count=1})
            }
            $script:playerQueueCounts.Add([int]$queue.Count)
            [pscustomobject]@{
                sessionId=$SessionId;planetId=104;movementState='Walk';speed=[double]0;coreEnergy=[long]100000
                handcraftQueue=[object[]]$queue
                inventory=@(
                    [pscustomobject]@{itemId=$script:materialItemId;count=[int]$materialInventory},
                    [pscustomobject]@{itemId=1101;count=[int]$firstInputInventory},
                    [pscustomobject]@{itemId=2011;count=[int]$craftedInventory}
                )
                stateHash=("player-state-{0}" -f $script:playerReads)
            }
        }
        inspect_factory_entity {
            $script:sourceReads++
            $available = if ($script:materialFailure -ceq 'source_missing') { 0 } else { 20 }
            if ($script:sourceReads -gt 1) { $available -= $script:materialCount }
            [pscustomobject]@{
                sessionId=$SessionId;planetId=104;objectId=[int]$Payload.objectId;objectKind='entity'
                itemId=2101;componentKind='storage';position=[pscustomobject]@{x=1;y=2;z=3}
                rotation=[pscustomobject]@{x=0;y=0;z=0;w=1};recipeId=0;connections=@()
                storageConfiguration=[pscustomobject]@{capacity=30};stateHash=("storage-state-{0}" -f $script:sourceReads)
                buffers=@([pscustomobject]@{itemId=$script:materialItemId;role='storage';countUnit='items';unitsPerItem=1;count=[int]$available})
            }
        }
        prepare_transfer {
            $budgetItemId=$script:materialItemId;$budgetCount=$script:materialCount;$direction='storage-to-player'
            if ($script:materialFailure -ceq 'transfer_budget') { $budgetCount++ }
            [pscustomobject]@{
                prepared=$true;commitAllowedNow=$true;planToken='private-transfer-token';actionKind='transfer'
                sourceObjectId=[int]$Payload.storageEntityId;destinationObjectId=$null
                itemBudget=@([pscustomobject]@{itemId=$budgetItemId;count=$budgetCount;direction=$direction})
            }
        }
        prepare_handcraft {
            $firstInputId=1101
            if ($script:materialFailure -ceq 'craft_budget') { $firstInputId=1999 }
            [pscustomobject]@{
                prepared=$true;commitAllowedNow=$true;planToken='private-handcraft-token';actionKind='handcraft'
                itemBudget=@(
                    [pscustomobject]@{itemId=$firstInputId;count=1;direction='input'},
                    [pscustomobject]@{itemId=$script:materialItemId;count=1;direction='input'},
                    [pscustomobject]@{itemId=2011;count=1;direction='output'}
                )
            }
        }
        prepare_save {
            $script:savePayload=$Payload
            [pscustomobject]@{prepared=$true;commitAllowedNow=$true;planToken='private-save-token';actionKind='save';itemBudget=@()}
        }
        { $_ -in @('commit_transfer','commit_handcraft','commit_save') } {
            $script:commitMethods.Add($Method)
            if ($Method -ceq 'commit_save' -and $script:materialFailure -ceq 'save_commit_lost') {
                $script:hiddenPossibleActionId='possible-save-action-3'
                throw 'Fixture response lost after commit may have reached the server.'
            }
            $sequence=$script:commitMethods.Count
            $kind=switch($Method){commit_transfer{'transfer'}commit_handcraft{'handcraft'}commit_save{'save'}}
            $actionId=("fixture-{0}-action-{1}" -f $kind,$sequence)
            $completedTick=switch($Method){commit_transfer{80442810}commit_handcraft{80442850}commit_save{80442879}}
            $commitRecord=[pscustomobject]@{method=$Method;actionId=$actionId;completedAtGameTick=$completedTick}
            if ($Method -ceq 'commit_transfer') {
                $commitRecord | Add-Member -NotePropertyName beforeTargetAmount -NotePropertyValue 20
                $commitRecord | Add-Member -NotePropertyName afterTargetAmount -NotePropertyValue (20 - $script:materialCount)
            }
            $script:fixtureCommits.Add($commitRecord)
            [pscustomobject]@{accepted=$true;idempotentReplay=$false;actionId=$actionId}
        }
        get_action_result {
            $matches=@($script:fixtureCommits | Where-Object { $_.actionId -ceq $Payload.actionId })
            if ($matches.Count -ne 1) { throw 'Terminal observation must match one original accepted action.' }
            $script:terminalActionIds.Add([string]$Payload.actionId)
            $terminal=[ordered]@{
                actionId=$Payload.actionId;terminal=$true;succeeded=$true
                completedAtGameTick=$matches[0].completedAtGameTick
            }
            if ($matches[0].method -ceq 'commit_transfer') {
                $terminal.beforeTargetAmount=$matches[0].beforeTargetAmount
                $terminal.afterTargetAmount=$matches[0].afterTargetAmount
            }
            [pscustomobject]$terminal
        }
        default { throw "Unexpected material fixture method $Method" }
    }
    [pscustomobject]@{success=$true;result=$result}
}

$materialArguments = @{
    SessionId='fixture-session';PlanetId=104;GameVersion='fixture-version';StorageEntityId=3051
    MaterialItemId=1301;MaterialCount=1;RecipeId=85;CraftCount=1;AcceptedBefore=7;AuditWindowLimit=10;TimeoutSeconds=1
    ValidateCraftPlan={
        param($plan)
        $budget=@($plan.itemBudget)
        if ($plan.actionKind -cne 'handcraft' -or $budget.Count -ne 3) { return $false }
        foreach ($expected in @(
            [pscustomobject]@{itemId=1101;count=1;direction='input'},
            [pscustomobject]@{itemId=1301;count=1;direction='input'},
            [pscustomobject]@{itemId=2011;count=1;direction='output'}
        )) {
            $rows=@($budget | Where-Object { $_.itemId -eq $expected.itemId })
            if ($rows.Count -ne 1 -or $rows[0].count -ne $expected.count -or
                $rows[0].direction -cne $expected.direction) { return $false }
        }
        return $true
    }
    ValidateCraftReadback={
        param($beforePlayer,$afterPlayer,$terminal)
        $script:craftReadbackCalls++
        if ($script:materialFailure -ceq 'multi_callback') { return @($true,$true) }
        $beforeFirstInput=Get-SpherewrightInventoryCount -PlayerState $beforePlayer -ItemId 1101
        $afterFirstInput=Get-SpherewrightInventoryCount -PlayerState $afterPlayer -ItemId 1101
        $beforeSecondInput=Get-SpherewrightInventoryCount -PlayerState $beforePlayer -ItemId $script:materialItemId
        $afterSecondInput=Get-SpherewrightInventoryCount -PlayerState $afterPlayer -ItemId $script:materialItemId
        $beforeOutput=Get-SpherewrightInventoryCount -PlayerState $beforePlayer -ItemId 2011
        $afterOutput=Get-SpherewrightInventoryCount -PlayerState $afterPlayer -ItemId 2011
        return [bool]($terminal.terminal -eq $true -and $terminal.succeeded -eq $true -and
            $afterFirstInput -eq ($beforeFirstInput - 1) -and
            $afterSecondInput -eq ($beforeSecondInput - 1) -and
            $afterOutput -eq ($beforeOutput + 1))
    }
    RecordEvidence={param($row) $script:recordedMaterialSummary=$row}
}
function Invoke-MaterialFailure([string]$Failure) {
    Reset-MaterialStage $Failure
    try { Invoke-SpherewrightMaterialHandcraftAndSave @materialArguments | Out-Null } catch { $script:materialError=$_ }
    return $script:materialError
}
function Assert-MaterialCommitPrefix([int]$Count, [string]$Name) {
    $commitIds=@($script:fixtureCommits | ForEach-Object actionId)
    $terminalIds=@($script:terminalActionIds)
    Assert-MaterialStage ($script:commitMethods.Count -eq $Count) "$Name has the exact attempted commit count"
    Assert-MaterialStage ($terminalIds.Count -le $commitIds.Count -and
        @($terminalIds | Where-Object { $commitIds -notcontains $_ }).Count -eq 0) "$Name terminals belong to their original commits"
}

Reset-MaterialStage
$materialSummary=Invoke-SpherewrightMaterialHandcraftAndSave @materialArguments
$materialCommitIds=@($script:fixtureCommits | ForEach-Object actionId)
$materialTerminalIds=@($script:terminalActionIds)
$materialMethods=@($script:commitMethods)
Assert-MaterialStage ($materialSummary.result -ceq 'completed' -and $materialSummary.acceptedDelta -eq 3 -and
    $materialSummary.acceptedAfter -eq 10 -and $materialSummary.frozen) 'one transfer, handcraft and save consume three audit slots'
Assert-MaterialStage ($materialSummary.auditWindowLimit -eq 10 -and $script:recordedMaterialSummary.auditWindowLimit -eq 10) 'explicit ten-write limit is preserved in result and evidence'
Assert-MaterialStage (($materialMethods -join ',') -ceq 'commit_transfer,commit_handcraft,commit_save' -and
    $materialCommitIds.Count -eq 3 -and @($materialCommitIds | Sort-Object -Unique).Count -eq 3) 'three distinct accepted actions occur in order'
Assert-MaterialStage (($materialCommitIds -join ',') -ceq ($materialTerminalIds -join ',')) 'every terminal observes its same original action'
Assert-MaterialStage ($script:savePayload.expectedRevision -eq 83 -and $script:revisionBeforeSave -eq 83 -and
    $materialSummary.revision -eq 157 -and $materialSummary.revision -ne ($script:savePayload.expectedRevision + 1)) 'save uses actual non-plus-one revisions'
Assert-MaterialStage ($materialSummary.durableThroughSequence -eq 101 -and $script:journalReads -eq 2) 'saved summary requires the fresh durable Journal boundary'
Assert-MaterialStage ($script:playerQueueCounts[0] -eq 0 -and $script:playerQueueCounts[2] -eq 0) 'empty forge queue permits transfer and verified handcraft completion'
Assert-MaterialStage ($script:methods -contains 'prepare_transfer' -and $script:methods -contains 'prepare_handcraft' -and
    $script:methods -contains 'prepare_save' -and $script:gameCalls -eq 0) 'offline transport fixture covers the complete flow with zero game calls'
$summaryJson=ConvertTo-Json -InputObject $materialSummary -Depth 12 -Compress
$recordJson=ConvertTo-Json -InputObject $script:recordedMaterialSummary -Depth 12 -Compress
Assert-MaterialStage ($script:recordedMaterialSummary.acceptedAfter -eq 10 -and
    $summaryJson -notmatch 'private-(transfer|handcraft|save)-token' -and
    $recordJson -notmatch 'private-(transfer|handcraft|save)-token') 'returned and recorded summaries never expose plan tokens'
Assert-MaterialCommitPrefix 3 'completed flow'

foreach ($boundary in @(
    [pscustomobject]@{limit=20;before=16;after=19;frozen=$false},
    [pscustomobject]@{limit=20;before=17;after=20;frozen=$true},
    [pscustomobject]@{limit=50;before=46;after=49;frozen=$false},
    [pscustomobject]@{limit=50;before=47;after=50;frozen=$true}
)) {
    $windowArguments = @{}; foreach ($key in $materialArguments.Keys) { $windowArguments[$key] = $materialArguments[$key] }
    $windowArguments.AuditWindowLimit = $boundary.limit
    $windowArguments.AcceptedBefore = $boundary.before
    Reset-MaterialStage
    $windowSummary = Invoke-SpherewrightMaterialHandcraftAndSave @windowArguments
    Assert-MaterialStage ($windowSummary.acceptedDelta -eq 3 -and $windowSummary.acceptedAfter -eq $boundary.after -and
        $windowSummary.frozen -eq $boundary.frozen -and $windowSummary.auditWindowLimit -eq $boundary.limit -and
        $script:recordedMaterialSummary.auditWindowLimit -eq $boundary.limit -and $script:commitMethods.Count -eq 3) "material stage exact $($boundary.limit)-write boundary from $($boundary.before)"
}
foreach ($limit in @(20,50)) {
    $windowArguments = @{}; foreach ($key in $materialArguments.Keys) { $windowArguments[$key] = $materialArguments[$key] }
    $windowArguments.AuditWindowLimit = $limit
    $windowArguments.AcceptedBefore = $limit - 1
    Reset-MaterialStage
    $errorRecord = $null
    try { Invoke-SpherewrightMaterialHandcraftAndSave @windowArguments | Out-Null } catch { $errorRecord = $_ }
    Assert-MaterialStage ($null -ne $errorRecord -and $script:methods.Count -eq 0 -and $script:commitMethods.Count -eq 0) "material stage $limit-write window reserves all three slots before any read"
}
foreach ($limit in @(20,50)) {
    $windowArguments = @{}; foreach ($key in $materialArguments.Keys) { $windowArguments[$key] = $materialArguments[$key] }
    $windowArguments.AuditWindowLimit = $limit
    $windowArguments.AcceptedBefore = $limit - 2
    Reset-MaterialStage
    $errorRecord = $null
    try { Invoke-SpherewrightMaterialHandcraftAndSave @windowArguments | Out-Null } catch { $errorRecord = $_ }
    Assert-MaterialStage ($null -ne $errorRecord -and $script:methods.Count -eq 0 -and $script:commitMethods.Count -eq 0) "material stage $limit-2 still cannot fit three slots before any read"
}
$defaultMaterialArguments = @{}; foreach ($key in $materialArguments.Keys) { $defaultMaterialArguments[$key] = $materialArguments[$key] }
$defaultMaterialArguments.Remove('AuditWindowLimit')
$defaultMaterialArguments.AcceptedBefore = 17
Reset-MaterialStage
$defaultMaterialSummary = Invoke-SpherewrightMaterialHandcraftAndSave @defaultMaterialArguments
Assert-MaterialStage ($defaultMaterialSummary.acceptedDelta -eq 3 -and $defaultMaterialSummary.acceptedAfter -eq 20 -and
    $defaultMaterialSummary.frozen -and $defaultMaterialSummary.auditWindowLimit -eq 20 -and
    $script:recordedMaterialSummary.auditWindowLimit -eq 20 -and $script:commitMethods.Count -eq 3) 'omitted material limit defaults to 20 and exactly fills 17 to 20'
$defaultMaterialArguments.AcceptedBefore = 18
Reset-MaterialStage
$defaultMaterialBudgetError = $null
try { Invoke-SpherewrightMaterialHandcraftAndSave @defaultMaterialArguments | Out-Null } catch { $defaultMaterialBudgetError = $_ }
Assert-MaterialStage ($null -ne $defaultMaterialBudgetError -and $script:methods.Count -eq 0 -and $script:commitMethods.Count -eq 0) 'default 20 material window rejects 18 before any request when three slots do not fit'

$errorRecord=Invoke-MaterialFailure 'forge_queue'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 0 -and
    $script:methods -notcontains 'prepare_transfer') 'nonempty initial forge queue stops before any prepare or commit'
Assert-MaterialCommitPrefix 0 'nonempty forge queue'

$errorRecord=Invoke-MaterialFailure 'source_missing'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 0 -and
    $script:methods -notcontains 'prepare_transfer') 'missing source material creates zero commits'
Assert-MaterialCommitPrefix 0 'missing source'

$errorRecord=Invoke-MaterialFailure 'transfer_budget'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 0 -and
    $script:methods -contains 'prepare_transfer' -and $script:methods -notcontains 'prepare_handcraft') 'mismatched transfer budget stops before any accepted action'
Assert-MaterialCommitPrefix 0 'transfer budget mismatch'

$errorRecord=Invoke-MaterialFailure 'craft_budget'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 1 -and
    $errorRecord.Exception.Data['spherewrightStagePhase'] -ceq 'handcraft') 'mismatched forge budget retains only the accepted transfer prefix'
Assert-MaterialStage (@($script:commitMethods | Where-Object {$_ -ceq 'commit_transfer'}).Count -eq 1 -and
    $script:commitMethods -notcontains 'commit_handcraft' -and $script:commitMethods -notcontains 'commit_save') 'mismatched forge budget commits neither craft nor save'
Assert-MaterialStage (@($errorRecord.Exception.Data['spherewrightPriorActionIds']).Count -eq 1) 'mismatched forge budget retains the original transfer action ID'
Assert-MaterialCommitPrefix 1 'forge budget mismatch'

$errorRecord=Invoke-MaterialFailure 'craft_readback'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 2 -and
    $errorRecord.Exception.Data['spherewrightStagePhase'] -ceq 'handcraft_readback') 'failed handcraft queue readback retains both accepted actions'
Assert-MaterialStage (@($script:commitMethods | Where-Object {$_ -ceq 'commit_handcraft'}).Count -eq 1 -and
    $script:commitMethods -notcontains 'commit_save') 'failed handcraft readback neither replays craft nor saves'
Assert-MaterialCommitPrefix 2 'handcraft readback failure'

$errorRecord=Invoke-MaterialFailure 'multi_callback'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 2 -and
    $errorRecord.Exception.Data['spherewrightStagePhase'] -ceq 'handcraft_readback' -and $script:craftReadbackCalls -eq 1) 'non-unique callback booleans fail closed after the accepted craft'
Assert-MaterialStage (@($script:commitMethods | Where-Object {$_ -ceq 'commit_handcraft'}).Count -eq 1 -and
    $script:commitMethods -notcontains 'commit_save') 'non-unique callback does not replay or save'
Assert-MaterialCommitPrefix 2 'non-unique callback result'

$errorRecord=Invoke-MaterialFailure 'save_commit_lost'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightCommitMayHaveBeenAccepted'] -eq $true -and
    $null -eq $errorRecord.Exception.Data['spherewrightCommitAccepted'] -and
    $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 2 -and
    $errorRecord.Exception.Data['spherewrightDoNotReplayStage'] -eq $true) 'lost save response remains uncertain and preserves only the known accepted prefix'
Assert-MaterialStage ($script:hiddenPossibleActionId -ne '' -and
    @($script:commitMethods | Where-Object {$_ -ceq 'commit_save'}).Count -eq 1) 'uncertain save commit is attempted once and never resubmitted'
Assert-MaterialCommitPrefix 3 'lost save commit'

$errorRecord=Invoke-MaterialFailure 'journal_pending'
Assert-MaterialStage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStagePhase'] -ceq 'save_readback' -and
    $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 3 -and $script:journalReads -eq 2) 'pending Journal blocks durable completion after the accepted save'
Assert-MaterialStage ($null -eq $script:recordedMaterialSummary -and
    @($script:commitMethods | Where-Object {$_ -ceq 'commit_save'}).Count -eq 1) 'pending Journal cannot emit a completed evidence summary or replay save'
Assert-MaterialCommitPrefix 3 'pending Journal'
Assert-MaterialStage ($script:gameCalls -eq 0) 'all material-stage cases remain offline transport fixtures'

[pscustomobject]@{
    passed=$script:checks;storageChecks=$script:storageChecks;materialChecks=$script:materialChecks
    gameCalls=$script:gameCalls;successfulFixtureRequests=14;materialFixtureRequests=$script:methods.Count
} | ConvertTo-Json -Compress
