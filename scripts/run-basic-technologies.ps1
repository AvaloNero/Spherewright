[CmdletBinding()]
param(
    [ValidateRange(30, 600)][int]$ActionTimeoutSeconds = 240,
    [ValidateRange(30, 600)][int]$ResearchTimeoutSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')

$session = Get-SpherewrightOwnedSession
$sessionId = [string]$session.sessionId
$planetId = [int]$session.localPlanetId

function Read-BridgeResult([string]$method, [hashtable]$payload) {
    Get-SpherewrightBridgeResult -Response (
        Invoke-SpherewrightBridgeRequest -Method $method -SessionId $sessionId -Payload $payload
    ) -Operation $method
}

function Get-PlayerSnapshot {
    Read-BridgeResult 'get_player_state' @{ planetId = $planetId }
}

function Get-ProgressionSnapshot {
    Read-BridgeResult 'get_progression_state' @{ planetId = $planetId }
}

function Get-InventoryCount([object]$player, [int]$itemId) {
    $entry = $player.inventory | Where-Object itemId -eq $itemId | Select-Object -First 1
    if ($null -eq $entry) { return 0 }
    return [int]$entry.count
}

function Get-NearestVein([string]$resourceType) {
    $page = Read-BridgeResult 'list_resource_nodes' @{
        planetId = $planetId
        kind = 'vein'
        resourceType = $resourceType
        limit = 100
    }
    $node = $page.nodes | Sort-Object distanceFromPlayer | Select-Object -First 1
    if ($null -eq $node) {
        throw "No $resourceType vein exists in the current owned-world snapshot."
    }
    return $node
}

function Invoke-MoveNear([object]$destination) {
    $player = Get-PlayerSnapshot
    Invoke-SpherewrightNormalAction `
        -PrepareMethod 'prepare_move' `
        -CommitMethod 'commit_move' `
        -SessionId $sessionId `
        -PlanetId $planetId `
        -TimeoutSeconds $ActionTimeoutSeconds `
        -PreparePayload @{
            planetId = $planetId
            target = @{
                x = [double]$destination.position.x
                y = [double]$destination.position.y
                z = [double]$destination.position.z
            }
            arrivalTolerance = 3.0
            expectedPlayerStateHash = [string]$player.stateHash
            stateHashVersion = 1
        }
}

function Ensure-InHarvestRange([string]$resourceType) {
    $node = Get-NearestVein $resourceType
    $attempt = 0
    # DSP's ordinary mining order can acquire a vein from this range and then
    # perform its own short approach. Avoid ordering the player onto the vein
    # centre, which is not always a reachable movement target.
    while ([double]$node.distanceFromPlayer -gt 18.0 -and $attempt -lt 3) {
        $null = Invoke-MoveNear $node
        $node = Get-NearestVein $resourceType
        $attempt++
    }
    if ([double]$node.distanceFromPlayer -gt 18.0) {
        throw "Could not walk within ordinary harvesting range of the nearest $resourceType vein."
    }
    return $node
}

function Ensure-RawOre([int]$itemId, [string]$resourceType, [int]$targetCount) {
    $player = Get-PlayerSnapshot
    $deficit = $targetCount - (Get-InventoryCount $player $itemId)
    if ($deficit -le 0) { return $null }

    $node = Ensure-InHarvestRange $resourceType
    $live = Read-BridgeResult 'inspect_resource_node' @{
        planetId = $planetId
        kind = 'vein'
        nodeId = [int]$node.nodeId
    }
    $player = Get-PlayerSnapshot
    return Invoke-SpherewrightNormalAction `
        -PrepareMethod 'prepare_harvest' `
        -CommitMethod 'commit_harvest' `
        -SessionId $sessionId `
        -PlanetId $planetId `
        -TimeoutSeconds $ActionTimeoutSeconds `
        -PreparePayload @{
            planetId = $planetId
            resourceKind = 'vein'
            nodeId = [int]$live.nodeId
            requestedYieldCount = $deficit
            expectedResourceStateHash = [string]$live.stateHash
            expectedPlayerStateHash = [string]$player.stateHash
            stateHashVersion = 1
        }
}

$catalog = Read-BridgeResult 'get_recipe_catalog' @{ planetId = $planetId }

function Ensure-HandcraftedItem([int]$itemId, [string]$itemName, [int]$targetCount) {
    $player = Get-PlayerSnapshot
    $deficit = $targetCount - (Get-InventoryCount $player $itemId)
    if ($deficit -le 0) { return $null }

    $recipe = $catalog.recipes |
        Where-Object { $_.unlocked -and $_.handcraft -and @($_.outputs | Where-Object itemId -eq $itemId).Count -gt 0 } |
        Select-Object -First 1
    if ($null -eq $recipe) {
        throw "No currently unlocked handcraft recipe produces $itemName ($itemId)."
    }
    $output = $recipe.outputs | Where-Object itemId -eq $itemId | Select-Object -First 1
    $executions = [int][Math]::Ceiling($deficit / [double]$output.count)
    $player = Get-PlayerSnapshot
    return Invoke-SpherewrightNormalAction `
        -PrepareMethod 'prepare_handcraft' `
        -CommitMethod 'commit_handcraft' `
        -SessionId $sessionId `
        -PlanetId $planetId `
        -TimeoutSeconds $ActionTimeoutSeconds `
        -PreparePayload @{
            planetId = $planetId
            recipeId = [int]$recipe.recipeId
            count = $executions
            expectedPlayerStateHash = [string]$player.stateHash
            stateHashVersion = 1
        }
}

function Complete-ItemTechnology([int]$techId) {
    $progression = Get-ProgressionSnapshot
    $technology = $progression.technologies | Where-Object techId -eq $techId | Select-Object -First 1
    if ($null -eq $technology) { throw "Technology $techId is absent from the runtime catalog." }
    if ($technology.unlocked) {
        return [pscustomobject]@{ selection = $null; technology = $technology }
    }
    if ($technology.isLabTech) {
        throw "Technology $techId is a matrix-lab technology, not an item bootstrap technology."
    }

    foreach ($requirement in $technology.itemRequirements) {
        $null = Ensure-HandcraftedItem `
            -itemId ([int]$requirement.itemId) `
            -itemName ([string]$requirement.name) `
            -targetCount ([int]$requirement.requiredItemCount)
    }

    $progression = Get-ProgressionSnapshot
    $selection = Invoke-SpherewrightNormalAction `
        -PrepareMethod 'prepare_select_research' `
        -CommitMethod 'commit_select_research' `
        -SessionId $sessionId `
        -PlanetId $planetId `
        -TimeoutSeconds 30 `
        -PreparePayload @{
            planetId = $planetId
            techId = $techId
            expectedProgressionStateHash = [string]$progression.stateHash
            stateHashVersion = 1
        }

    $deadline = (Get-Date).AddSeconds($ResearchTimeoutSeconds)
    do {
        $progression = Get-ProgressionSnapshot
        $technology = $progression.technologies | Where-Object techId -eq $techId | Select-Object -First 1
        if ($technology.unlocked) { break }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)

    if (-not $technology.unlocked) {
        throw "Technology $techId did not complete within $ResearchTimeoutSeconds seconds."
    }
    return [pscustomobject]@{ selection = $selection.result; technology = $technology }
}

# The budgets cover the exact three current-runtime item technologies:
# 30 circuit boards, 20 gears and 10 magnetic coils. Their ordinary nested
# handcraft recipes consume 60 iron ore and 20 copper ore in total.
$ironHarvest = Ensure-RawOre -itemId 1001 -resourceType 'Iron' -targetCount 60
$copperHarvest = Ensure-RawOre -itemId 1002 -resourceType 'Copper' -targetCount 20

$results = foreach ($techId in @(1201, 1401, 1601)) {
    Complete-ItemTechnology $techId
}

[pscustomobject]@{
    sessionId = $sessionId
    planetId = $planetId
    ironHarvest = $(if ($null -eq $ironHarvest) { $null } else { $ironHarvest.result })
    copperHarvest = $(if ($null -eq $copperHarvest) { $null } else { $copperHarvest.result })
    technologies = $results
    finalPlayer = Get-PlayerSnapshot
    finalProgression = Get-ProgressionSnapshot
} | ConvertTo-Json -Depth 16
