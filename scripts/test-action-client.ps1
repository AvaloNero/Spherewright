# Offline regression; no descriptor discovery or game request is made.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')
$script:checks = 0
function Assert-Count($State, [int]$Id, [long]$Expected) {
    $actual = Get-SpherewrightInventoryCount -PlayerState $State -ItemId $Id
    if ($actual -ne $Expected) { throw "Inventory count expected $Expected, got $actual." }
    $script:checks++
}
function Assert-Rejected([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw 'Malformed or unknown inventory was silently accepted.' }
    $script:checks++
}
Assert-Count ([pscustomobject]@{inventory=@()}) 1104 0
$before = '{"inventory":[{"itemId":2012,"count":2}]}' | ConvertFrom-Json
$after = '{"inventory":[{"itemId":1104,"count":1},{"itemId":2012,"count":3}]}' | ConvertFrom-Json
Assert-Count $before 2012 2
Assert-Count $before 1104 0
Assert-Count $after 2012 3
Assert-Count $after 1104 1
Assert-Count ('{"inventory":[{"itemId":1104,"count":2},{"itemId":1104,"count":3}]}'|ConvertFrom-Json) 1104 5
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState ([pscustomobject]@{}) -ItemId 1104 }
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState ([pscustomobject]@{inventory=$null}) -ItemId 1104 }
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState ([pscustomobject]@{inventory=@($null)}) -ItemId 1104 }
foreach ($json in @(
    '{"inventory":[{"itemId":1104}]}',
    '{"inventory":[{"itemId":1104,"count":null}]}',
    '{"inventory":[{"itemId":1104,"count":-1}]}',
    '{"inventory":[{"itemId":1104,"count":1.5}]}',
    '{"inventory":[{"itemId":1104,"count":"1"}]}',
    '{"inventory":[{"itemId":1104,"count":true}]}',
    '{"inventory":[{"count":1}]}',
    '{"inventory":[{"itemId":0,"count":1}]}',
    '{"inventory":[{"itemId":"1104","count":1}]}',
    '{"inventory":[{"itemId":1104,"count":2147483647},{"itemId":1104,"count":1}]}',
    '{"inventory":[{"itemId":9999,"count":null}]}'
)) {
    $state = $json | ConvertFrom-Json
    Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState $state -ItemId 1104 }
}
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState $before -ItemId 0 }

# Deterministic clock/transport stubs exercise the existing caller, not a second
# action executor. No runtime descriptor, pipe, game process or real sleep.
function Assert-Action([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw "Action-client regression: $Label" }
    $script:checks++
}
function Reset-ActionStub([object[]]$States, [int]$ReadMilliseconds = 0) {
    $script:actionStubStates = [Collections.Generic.Queue[object]]::new()
    foreach ($state in $States) { $script:actionStubStates.Enqueue($state) }
    $script:actionStubCalls = [Collections.Generic.List[object]]::new()
    $script:actionStubSleeps = [Collections.Generic.List[int]]::new()
    $script:actionStubTime = [datetime]'2026-01-01T00:00:00Z'
    $script:actionStubReadMilliseconds = $ReadMilliseconds
    $script:actionStubTransportFailure = $false
}
function Get-Date { $script:actionStubTime }
function Start-Sleep([int]$Milliseconds) {
    $script:actionStubSleeps.Add($Milliseconds)
    $script:actionStubTime = $script:actionStubTime.AddMilliseconds($Milliseconds)
}
function Invoke-SpherewrightBridgeRequest([string]$Method, [string]$SessionId, [hashtable]$Payload) {
    $script:actionStubCalls.Add([pscustomobject]@{method=$Method;sessionId=$SessionId;payload=$Payload})
    $result = switch ($Method) {
        'prepare_build' { [pscustomobject]@{prepared=$true;commitAllowedNow=$true;planToken='offline-test-placeholder'} }
        'commit_build' { [pscustomobject]@{accepted=$true;actionId='offline-action'} }
        'get_action_result' {
            if ($script:actionStubTransportFailure) { throw 'offline transport failure' }
            $script:actionStubTime = $script:actionStubTime.AddMilliseconds($script:actionStubReadMilliseconds)
            if ($script:actionStubStates.Count) { $script:actionStubStates.Dequeue() }
            else { [pscustomobject]@{terminal=$false;succeeded=$false;state='waiting_for_game'} }
        }
        default { throw "Unexpected offline method: $Method" }
    }
    [pscustomobject]@{success=$true;result=$result}
}
function Assert-SameActionReads {
    $reads = @($script:actionStubCalls | Where-Object method -EQ 'get_action_result')
    Assert-Action ($reads.Count -gt 0) 'at least one action observation'
    Assert-Action (@($reads | Where-Object { $_.sessionId -cne 'offline-session' -or $_.payload.actionId -cne 'offline-action' }).Count -eq 0) 'same action and session on every observation'
}
$pending = [pscustomobject]@{terminal=$false;succeeded=$false;state='waiting_for_game'}
$success = [pscustomobject]@{terminal=$true;succeeded=$true;state='completed'}
Reset-ActionStub @($success)
$result = Wait-SpherewrightAction -ActionId offline-action -SessionId offline-session
Assert-Action ($result.terminal -and $result.succeeded) 'immediate terminal returned unchanged'
Assert-Action ($script:actionStubCalls.Count -eq 1 -and $script:actionStubSleeps.Count -eq 0) 'no sleep after terminal'
Assert-SameActionReads

Reset-ActionStub @($pending, $success)
$intent = [guid]'00000000-0000-0000-0000-000000000001'
$result = Invoke-SpherewrightNormalAction -PrepareMethod prepare_build -CommitMethod commit_build -PreparePayload @{buildingItemId=2001} -SessionId offline-session -PlanetId 104 -IdempotencyKey $intent
Assert-Action ($result.result.succeeded) 'short action completed'
Assert-Action (($script:actionStubCalls.method -join ',') -ceq 'prepare_build,commit_build,get_action_result,get_action_result') 'prepare and commit occur exactly once and in order'
Assert-Action (($script:actionStubSleeps -join ',') -ceq '250') 'first pending observation retains quick cadence'
Assert-Action ($script:actionStubCalls[1].payload.idempotencyKey -ceq $intent.ToString('D')) 'original idempotency key preserved'
Assert-Action ($script:actionStubCalls[1].payload.planToken -ceq 'offline-test-placeholder') 'only the original prepared plan committed'
Assert-SameActionReads

Reset-ActionStub @($pending, $pending, $pending, $pending, $pending, $pending, $success)
$result = Wait-SpherewrightAction -ActionId offline-action -SessionId offline-session -TimeoutSeconds 30
Assert-Action ($result.succeeded) 'long action completed'
Assert-Action (($script:actionStubSleeps -join ',') -ceq '250,500,1000,2000,2000,2000') 'exponential observation backoff capped at two seconds'
Assert-Action ($script:actionStubCalls.Count -eq 7) 'bounded long-action observation count'
Assert-Action (@($script:actionStubCalls | Where-Object method -NE 'get_action_result').Count -eq 0) 'wait never prepares or commits'
Assert-SameActionReads

Reset-ActionStub @($pending, [pscustomobject]@{terminal=$true;succeeded=$false;state='failed';message='offline blocked'})
$failure = $null
try { Wait-SpherewrightAction -ActionId offline-action -SessionId offline-session | Out-Null } catch { $failure=$_.Exception.Message }
Assert-Action ($failure -ceq 'Action offline-action ended as failed: offline blocked') 'terminal failure is propagated'
Assert-Action ($script:actionStubCalls.Count -eq 2 -and ($script:actionStubSleeps -join ',') -ceq '250') 'no further observation or sleep after failure'
Assert-SameActionReads

Reset-ActionStub @() 100
$failure = $null
try { Wait-SpherewrightAction -ActionId offline-action -SessionId offline-session -TimeoutSeconds 1 | Out-Null } catch { $failure=$_.Exception.Message }
Assert-Action ($failure -ceq 'Action offline-action did not reach a terminal state within 1 seconds.') 'original caller timeout and handle retained'
Assert-Action (($script:actionStubSleeps -join ',') -ceq '250,500') 'sleep never extends remaining deadline'
Assert-Action ($script:actionStubCalls.Count -eq 3) 'response crossing deadline stops without another sleep or request'
Assert-SameActionReads

Reset-ActionStub @()
$failure = $null
try { Wait-SpherewrightAction -ActionId offline-action -SessionId offline-session -TimeoutSeconds 1 | Out-Null } catch { $failure=$_.Exception.Message }
Assert-Action ($failure -ceq 'Action offline-action did not reach a terminal state within 1 seconds.') 'pending action expires without replay'
Assert-Action (($script:actionStubSleeps -join ',') -ceq '250,500,250') 'last sleep clipped to remaining deadline'
Assert-Action ($script:actionStubCalls.Count -eq 3) 'no poll after expired deadline'
Assert-SameActionReads

Reset-ActionStub @()
$script:actionStubTransportFailure = $true
$failure = $null
try { Wait-SpherewrightAction -ActionId offline-action -SessionId offline-session | Out-Null } catch { $failure=$_.Exception.Message }
Assert-Action ($failure -ceq 'offline transport failure') 'transport uncertainty propagated without retry'
Assert-Action ($script:actionStubCalls.Count -eq 1 -and $script:actionStubSleeps.Count -eq 0) 'transport error never resubmits or sleeps'
[pscustomobject]@{passed=$script:checks;gameCalls=0} | ConvertTo-Json -Compress
