Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightBridgeClient.ps1')

function Get-SpherewrightOwnedSession {
    [CmdletBinding()]
    param()

    $response = Invoke-SpherewrightBridgeRequest -Method 'get_session_state' -Payload @{}
    $session = Get-SpherewrightBridgeResult -Response $response -Operation 'get_session_state'
    if (-not $session.gameLoaded -or -not $session.ownedBySpherewright) {
        throw 'No Spherewright-owned ordinary game session is active.'
    }

    return $session
}

function Wait-SpherewrightAction {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ActionId,
        [Parameter(Mandatory)][string]$SessionId,
        [ValidateRange(1, 1800)][int]$TimeoutSeconds = 180
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $response = Invoke-SpherewrightBridgeRequest -Method 'get_action_result' -SessionId $SessionId -Payload @{
            actionId = $ActionId
        }
        $action = Get-SpherewrightBridgeResult -Response $response -Operation 'get_action_result'
        if ($action.terminal) {
            if (-not $action.succeeded) {
                throw "Action $ActionId ended as $($action.state): $($action.message)"
            }

            return $action
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    throw "Action $ActionId did not reach a terminal state within $TimeoutSeconds seconds."
}

function Invoke-SpherewrightNormalAction {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$PrepareMethod,
        [Parameter(Mandatory)][string]$CommitMethod,
        [Parameter(Mandatory)][hashtable]$PreparePayload,
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][int]$PlanetId,
        [ValidateRange(1, 1800)][int]$TimeoutSeconds = 180,
        [guid]$IdempotencyKey = [guid]::NewGuid()
    )

    $prepareResponse = Invoke-SpherewrightBridgeRequest -Method $PrepareMethod -SessionId $SessionId -Payload $PreparePayload
    $prepared = Get-SpherewrightBridgeResult -Response $prepareResponse -Operation $PrepareMethod
    if (-not $prepared.prepared -or [string]::IsNullOrWhiteSpace([string]$prepared.planToken)) {
        throw "$PrepareMethod did not issue an executable plan token."
    }

    if (-not $prepared.commitAllowedNow) {
        $codes = @($prepared.commitBlockers | ForEach-Object { $_.code }) -join ', '
        throw "$PrepareMethod is currently blocked: $codes"
    }

    $commitResponse = Invoke-SpherewrightBridgeRequest -Method $CommitMethod -SessionId $SessionId -Payload @{
        sessionId = $SessionId
        planetId = $PlanetId
        planToken = [string]$prepared.planToken
        idempotencyKey = $IdempotencyKey.ToString('D')
    }
    $committed = Get-SpherewrightBridgeResult -Response $commitResponse -Operation $CommitMethod
    $terminal = Wait-SpherewrightAction -ActionId ([string]$committed.actionId) -SessionId $SessionId -TimeoutSeconds $TimeoutSeconds
    return [pscustomobject]@{
        prepared = $prepared
        committed = $committed
        result = $terminal
    }
}
