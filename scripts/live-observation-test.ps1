[CmdletBinding()]
param(
    [int]$GalaxySeed = 24681357,
    [ValidateRange(20, 80)][int]$StarCount = 32,
    [ValidateRange(5, 120)][int]$BridgeReadyTimeoutSeconds = 60,
    [ValidateRange(15, 180)][int]$WorldLoadTimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$runtimeDirectory = Join-Path $env:LOCALAPPDATA 'Spherewright\runtime'
$bridgeDeadline = (Get-Date).AddSeconds($BridgeReadyTimeoutSeconds)
$descriptors = @()
do {
    $descriptors = @(Get-ChildItem -LiteralPath $runtimeDirectory -Filter 'bridge-*.json' -File -ErrorAction SilentlyContinue | Where-Object {
        try {
            $candidate = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
            $process = Get-Process -Id ([int]$candidate.processId) -ErrorAction Stop
            $process.ProcessName -eq 'DSPGAME' -and -not $process.HasExited
        } catch {
            $false
        }
    })
    if ($descriptors.Count -eq 1) { break }
    Start-Sleep -Milliseconds 500
} while ((Get-Date) -lt $bridgeDeadline)

if ($descriptors.Count -ne 1) {
    throw "Expected exactly one live Spherewright bridge descriptor, found $($descriptors.Count)."
}

$descriptor = Get-Content -LiteralPath $descriptors[0].FullName -Raw | ConvertFrom-Json

function Write-BridgeFrame {
    param(
        [Parameter(Mandatory)][IO.Stream]$Stream,
        [Parameter(Mandatory)][object]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $payload = [Text.Encoding]::UTF8.GetBytes($json)
    $header = [BitConverter]::GetBytes([int]$payload.Length)
    $Stream.Write($header, 0, $header.Length)
    $Stream.Write($payload, 0, $payload.Length)
    $Stream.Flush()
}

function Read-ExactBytes {
    param(
        [Parameter(Mandatory)][IO.Stream]$Stream,
        [Parameter(Mandatory)][int]$Count
    )

    $buffer = [byte[]]::new($Count)
    $offset = 0
    while ($offset -lt $Count) {
        $read = $Stream.Read($buffer, $offset, $Count - $offset)
        if ($read -eq 0) { throw 'Bridge connection closed during a frame.' }
        $offset += $read
    }

    return $buffer
}

function Read-BridgeFrame {
    param([Parameter(Mandatory)][IO.Stream]$Stream)

    $header = Read-ExactBytes -Stream $Stream -Count 4
    $length = [BitConverter]::ToInt32($header, 0)
    if ($length -lt 0 -or $length -gt 1048576) { throw "Invalid bridge frame length: $length" }
    $payload = Read-ExactBytes -Stream $Stream -Count $length
    return ([Text.Encoding]::UTF8.GetString($payload) | ConvertFrom-Json)
}

function Invoke-BridgeRequest {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][object]$Payload,
        [string]$SessionId
    )

    $pipe = [IO.Pipes.NamedPipeClientStream]::new(
        '.',
        [string]$descriptor.pipeName,
        [IO.Pipes.PipeDirection]::InOut,
        [IO.Pipes.PipeOptions]::None)
    try {
        $pipe.Connect(10000)
        $handshakeId = [guid]::NewGuid().ToString('D')
        Write-BridgeFrame -Stream $pipe -Value @{
            protocolVersion = 1
            messageType = 'handshake'
            requestId = $handshakeId
            payload = @{
                bridgeInstanceId = [string]$descriptor.bridgeInstanceId
                authToken = [string]$descriptor.authToken
                clientName = 'Spherewright.LiveObservationTest'
                clientVersion = '0.1.0'
            }
        }
        $handshake = Read-BridgeFrame -Stream $pipe
        if (-not $handshake.success) { throw 'Bridge handshake failed.' }

        $request = @{
            protocolVersion = 1
            messageType = 'request'
            requestId = [guid]::NewGuid().ToString('D')
            method = $Method
            payload = $Payload
        }
        if ($SessionId) { $request.sessionId = $SessionId }
        Write-BridgeFrame -Stream $pipe -Value $request
        return Read-BridgeFrame -Stream $pipe
    } finally {
        $pipe.Dispose()
    }
}

function Require-Success {
    param(
        [Parameter(Mandatory)][object]$Response,
        [Parameter(Mandatory)][string]$Operation
    )

    if (-not $Response.success) {
        throw "$Operation failed: $($Response.error.code): $($Response.error.message)"
    }

    return $Response.result
}

$prepareDeadline = (Get-Date).AddSeconds($WorldLoadTimeoutSeconds)
$prepareResponse = $null
do {
    $prepareResponse = Invoke-BridgeRequest -Method 'prepare_new_game' -Payload @{
        galaxySeed = $GalaxySeed
        starCount = $StarCount
    }
    if ($prepareResponse.success) { break }
    $prepareErrorCode = [string]$prepareResponse.error.code
    if (@('BRIDGE_NOT_READY', 'REQUEST_TIMEOUT') -notcontains $prepareErrorCode) {
        break
    }

    Start-Sleep -Milliseconds 500
} while ((Get-Date) -lt $prepareDeadline)

if ($null -eq $prepareResponse) {
    throw 'prepare_new_game returned no response.'
}
$prepared = Require-Success -Response $prepareResponse -Operation 'prepare_new_game'
if ($prepared.sandboxMode -ne $false -or $prepared.peacefulMode -ne $true -or [Math]::Abs([double]$prepared.resourceMultiplier - 1.0) -gt 0.0001) {
    throw 'Prepared world did not report peaceful 1x non-sandbox settings.'
}

$idempotencyKey = [guid]::NewGuid().ToString('D')
$commitResponse = Invoke-BridgeRequest -Method 'commit_new_game' -Payload @{
    planToken = [string]$prepared.planToken
    idempotencyKey = $idempotencyKey
}
$commit = Require-Success -Response $commitResponse -Operation 'commit_new_game'

$deadline = (Get-Date).AddSeconds($WorldLoadTimeoutSeconds)
$session = $null
do {
    Start-Sleep -Milliseconds 500
    $sessionResponse = Invoke-BridgeRequest -Method 'get_session_state' -Payload @{}
    $candidateSession = Require-Success -Response $sessionResponse -Operation 'get_session_state'
    if ($candidateSession.gameLoaded -and $candidateSession.ownedBySpherewright -and $candidateSession.localPlanetId) {
        $session = $candidateSession
        break
    }
} while ((Get-Date) -lt $deadline)

if ($null -eq $session) { throw 'The owned ordinary world did not become observable before the timeout.' }
if ($session.peacefulMode -ne 'confirmed_peaceful') { throw 'Peaceful mode was not confirmed.' }
if ($session.sandboxMode -ne 'confirmed_disabled') { throw 'Sandbox mode was not confirmed disabled.' }
if ([Math]::Abs([double]$session.resourceMultiplier - 1.0) -gt 0.0001) { throw 'Resource multiplier was not 1x.' }

$sessionId = [string]$session.sessionId
$planetId = [int]$session.localPlanetId
$planetPayload = @{ planetId = $planetId }

$player = Require-Success -Response (Invoke-BridgeRequest -Method 'get_player_state' -SessionId $sessionId -Payload $planetPayload) -Operation 'get_player_state'
$progression = Require-Success -Response (Invoke-BridgeRequest -Method 'get_progression_state' -SessionId $sessionId -Payload $planetPayload) -Operation 'get_progression_state'
$recipes = Require-Success -Response (Invoke-BridgeRequest -Method 'get_recipe_catalog' -SessionId $sessionId -Payload $planetPayload) -Operation 'get_recipe_catalog'
$resources = Require-Success -Response (Invoke-BridgeRequest -Method 'list_resource_nodes' -SessionId $sessionId -Payload @{
    planetId = $planetId
    limit = 5
}) -Operation 'list_resource_nodes'
$factory = Require-Success -Response (Invoke-BridgeRequest -Method 'list_factory_entities' -SessionId $sessionId -Payload @{
    planetId = $planetId
    limit = 5
}) -Operation 'list_factory_entities'
$power = Require-Success -Response (Invoke-BridgeRequest -Method 'get_power_summary' -SessionId $sessionId -Payload $planetPayload) -Operation 'get_power_summary'
$action = Require-Success -Response (Invoke-BridgeRequest -Method 'get_action_result' -Payload @{
    actionId = [string]$commit.actionId
}) -Operation 'get_action_result'

if ([int]$player.planetId -ne $planetId) { throw 'Player snapshot returned a different planet.' }
if ([int]$recipes.firstRedMatrixDependencies.targetItemId -le 0) { throw 'No runtime first-red-matrix target was identified.' }
if (@($recipes.firstRedMatrixDependencies.recipeIds).Count -eq 0) { throw 'The first-red-matrix dependency graph is empty.' }
if (@($resources.nodes).Count -eq 0) { throw 'The fresh local planet returned no resource nodes.' }

$firstResource = $resources.nodes[0]
$inspectedResource = Require-Success -Response (Invoke-BridgeRequest -Method 'inspect_resource_node' -SessionId $sessionId -Payload @{
    planetId = $planetId
    kind = [string]$firstResource.kind
    nodeId = [int]$firstResource.nodeId
}) -Operation 'inspect_resource_node'
if ([int]$inspectedResource.nodeId -ne [int]$firstResource.nodeId) { throw 'Live resource inspection returned a different node.' }

$cursorBindingRejected = $null
if ($resources.nextCursor) {
    $staleResponse = Invoke-BridgeRequest -Method 'list_resource_nodes' -SessionId $sessionId -Payload @{
        planetId = $planetId
        kind = 'vein'
        limit = 5
        cursor = [string]$resources.nextCursor
    }
    $cursorBindingRejected = (-not $staleResponse.success -and $staleResponse.error.code -eq 'STALE_CURSOR')
    if (-not $cursorBindingRejected) { throw 'A resource cursor was accepted under different filters.' }
}

$replay = Require-Success -Response (Invoke-BridgeRequest -Method 'commit_new_game' -Payload @{
    planToken = [string]$prepared.planToken
    idempotencyKey = $idempotencyKey
}) -Operation 'commit_new_game replay'
if (-not $replay.idempotentReplay -or $replay.actionId -ne $commit.actionId) {
    throw 'The repeated new-game commit did not replay the original action.'
}

[ordered]@{
    processId = [int]$descriptor.processId
    bridgeInstanceId = [string]$descriptor.bridgeInstanceId
    actionId = [string]$commit.actionId
    actionState = [string]$action.state
    actionTerminal = [bool]$action.terminal
    idempotentReplay = [bool]$replay.idempotentReplay
    sessionId = $sessionId
    planetId = $planetId
    gameTick = [long]$session.gameTick
    peacefulMode = [string]$session.peacefulMode
    sandboxMode = [string]$session.sandboxMode
    resourceMultiplier = [double]$session.resourceMultiplier
    inventoryItemKinds = @($player.inventory).Count
    handcraftQueueCount = @($player.handcraftQueue).Count
    technologyCount = @($progression.technologies).Count
    recipeCount = @($recipes.recipes).Count
    itemCount = @($recipes.items).Count
    redMatrixItemId = [int]$recipes.firstRedMatrixDependencies.targetItemId
    redMatrixDependencyRecipeCount = @($recipes.firstRedMatrixDependencies.recipeIds).Count
    firstResourceKind = [string]$firstResource.kind
    firstResourceType = [string]$firstResource.resourceType
    firstResourceRemaining = [int]$inspectedResource.remainingAmount
    resourcePageCount = @($resources.nodes).Count
    resourceCursorBindingRejected = $cursorBindingRejected
    factoryPageCount = @($factory.entities).Count
    powerNetworkCount = @($power.networks).Count
} | ConvertTo-Json -Depth 5
