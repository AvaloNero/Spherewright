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

function Get-PlayerSnapshot {
    Get-SpherewrightBridgeResult -Response (
        Invoke-SpherewrightBridgeRequest -Method 'get_player_state' -SessionId $sessionId -Payload @{
            planetId = $planetId
        }) -Operation 'get_player_state'
}

function Get-ProgressionSnapshot {
    Get-SpherewrightBridgeResult -Response (
        Invoke-SpherewrightBridgeRequest -Method 'get_progression_state' -SessionId $sessionId -Payload @{
            planetId = $planetId
        }) -Operation 'get_progression_state'
}

function Get-NearestVein([string]$resourceType) {
    $page = Get-SpherewrightBridgeResult -Response (
        Invoke-SpherewrightBridgeRequest -Method 'list_resource_nodes' -SessionId $sessionId -Payload @{
            planetId = $planetId
            kind = 'vein'
            resourceType = $resourceType
            limit = 100
        }) -Operation "list $resourceType veins"
    $node = $page.nodes | Sort-Object distanceFromPlayer | Select-Object -First 1
    if ($null -eq $node) {
        throw "No $resourceType vein exists in the current owned world snapshot."
    }

    return $node
}

function Invoke-Harvest([int]$nodeId, [int]$count) {
    $node = Get-SpherewrightBridgeResult -Response (
        Invoke-SpherewrightBridgeRequest -Method 'inspect_resource_node' -SessionId $sessionId -Payload @{
            planetId = $planetId
            kind = 'vein'
            nodeId = $nodeId
        }) -Operation "inspect vein $nodeId"
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
            nodeId = $nodeId
            requestedYieldCount = $count
            expectedResourceStateHash = [string]$node.stateHash
            expectedPlayerStateHash = [string]$player.stateHash
            stateHashVersion = 1
        }
}

function Invoke-SideWaypoint([object]$destination) {
    $player = Get-PlayerSnapshot
    $start = [System.Numerics.Vector3]::new(
        [float]$player.position.x,
        [float]$player.position.y,
        [float]$player.position.z)
    $finish = [System.Numerics.Vector3]::new(
        [float]$destination.position.x,
        [float]$destination.position.y,
        [float]$destination.position.z)
    $midpoint = ($start + $finish) / 2
    $normal = [System.Numerics.Vector3]::Normalize($midpoint)
    $direction = $finish - $start
    $side = [System.Numerics.Vector3]::Normalize(
        [System.Numerics.Vector3]::Cross($normal, $direction))
    $waypoint = [System.Numerics.Vector3]::Normalize($midpoint + ($side * 10)) * $start.Length()
    return Invoke-SpherewrightNormalAction `
        -PrepareMethod 'prepare_move' `
        -CommitMethod 'commit_move' `
        -SessionId $sessionId `
        -PlanetId $planetId `
        -TimeoutSeconds $ActionTimeoutSeconds `
        -PreparePayload @{
            planetId = $planetId
            target = @{ x = $waypoint.X; y = $waypoint.Y; z = $waypoint.Z }
            arrivalTolerance = 1.5
            expectedPlayerStateHash = [string]$player.stateHash
            stateHashVersion = 1
        }
}

$catalog = Get-SpherewrightBridgeResult -Response (
    Invoke-SpherewrightBridgeRequest -Method 'get_recipe_catalog' -SessionId $sessionId -Payload @{
        planetId = $planetId
    }) -Operation 'get_recipe_catalog'

function Invoke-HandcraftOutput([string]$outputName, [int]$outputCount) {
    $recipe = $catalog.recipes |
        Where-Object { $_.handcraft -and @($_.outputs | Where-Object name -eq $outputName).Count -gt 0 } |
        Select-Object -First 1
    if ($null -eq $recipe) {
        throw "No unlocked handcraft recipe produces $outputName in the current runtime catalog."
    }

    $output = $recipe.outputs | Where-Object name -eq $outputName | Select-Object -First 1
    $executions = [int][Math]::Ceiling($outputCount / [double]$output.count)
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

function Complete-Technology([string]$technologyName) {
    $progression = Get-ProgressionSnapshot
    $technology = $progression.technologies | Where-Object name -eq $technologyName | Select-Object -First 1
    if ($null -eq $technology) {
        throw "Technology $technologyName is absent from the current runtime catalog."
    }

    if (-not $technology.unlocked) {
        $selection = Invoke-SpherewrightNormalAction `
            -PrepareMethod 'prepare_select_research' `
            -CommitMethod 'commit_select_research' `
            -SessionId $sessionId `
            -PlanetId $planetId `
            -TimeoutSeconds 30 `
            -PreparePayload @{
                planetId = $planetId
                techId = [int]$technology.techId
                expectedProgressionStateHash = [string]$progression.stateHash
                stateHashVersion = 1
            }

        $deadline = (Get-Date).AddSeconds($ResearchTimeoutSeconds)
        do {
            $progression = Get-ProgressionSnapshot
            $technology = $progression.technologies |
                Where-Object techId -eq $technology.techId |
                Select-Object -First 1
            if ($technology.unlocked) {
                break
            }

            Start-Sleep -Milliseconds 500
        } while ((Get-Date) -lt $deadline)

        if (-not $technology.unlocked) {
            throw "Technology $technologyName did not complete within $ResearchTimeoutSeconds seconds."
        }

        return [pscustomobject]@{ selection = $selection.result; technology = $technology }
    }

    return [pscustomobject]@{ selection = $null; technology = $technology }
}

function Get-InventoryCount([object]$player, [string]$itemName) {
    $entry = $player.inventory | Where-Object name -eq $itemName | Select-Object -First 1
    if ($null -eq $entry) {
        return 0
    }

    return [int]$entry.count
}

function Wait-PlayerActionStateStable {
    $deadline = (Get-Date).AddSeconds(30)
    $previousHash = $null
    do {
        $player = Get-PlayerSnapshot
        if ($previousHash -and [string]$player.stateHash -eq $previousHash) {
            return $player
        }

        $previousHash = [string]$player.stateHash
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)

    throw 'Player action state did not stabilize after the newly created world finished loading.'
}

# Skip-prologue world creation can expose the owned GameData before Icarus has
# completed the ordinary landing transition. Preparing against two different
# landing positions is correctly stale, so wait for two identical public
# action hashes before the first mutable bootstrap observation.
$null = Wait-PlayerActionStateStable

$initialProgression = Get-ProgressionSnapshot
$electromagnetismState = $initialProgression.technologies |
    Where-Object name -eq '电磁学' |
    Select-Object -First 1
$blueMatrixState = $initialProgression.technologies |
    Where-Object name -eq '电磁矩阵' |
    Select-Object -First 1
if ($null -eq $electromagnetismState -or $null -eq $blueMatrixState) {
    throw 'The current runtime catalog does not contain the two expected early technologies.'
}

$copperHarvest = $null
$waypointMove = $null
$ironHarvest = $null
$firstCoils = $null
$electromagnetism = [pscustomobject]@{ selection = $null; technology = $electromagnetismState }
$secondCoils = $null
$boards = $null
$blueMatrixTechnology = [pscustomobject]@{ selection = $null; technology = $blueMatrixState }

if (-not $blueMatrixState.unlocked) {
    # Raw budgets are derived from the current runtime recipes: two batches of
    # 10 coils plus 10 circuit boards before tech 1002, or only the second
    # batch after tech 1001 is already complete. Existing partial products can
    # only make this conservative budget leave harmless raw material behind.
    $targetIron = if ($electromagnetismState.unlocked) { 20 } else { 30 }
    $targetCopper = if ($electromagnetismState.unlocked) { 10 } else { 15 }
    $player = Get-PlayerSnapshot
    $copperDeficit = [Math]::Max(0, $targetCopper - (Get-InventoryCount $player '铜矿'))
    if ($copperDeficit -gt 0) {
        $copper = Get-NearestVein 'Copper'
        $copperHarvest = Invoke-Harvest -nodeId ([int]$copper.nodeId) -count $copperDeficit
    }

    $player = Get-PlayerSnapshot
    $ironDeficit = [Math]::Max(0, $targetIron - (Get-InventoryCount $player '铁矿'))
    if ($ironDeficit -gt 0) {
        $iron = Get-NearestVein 'Iron'
        if ([double]$iron.distanceFromPlayer -gt 20) {
            $waypointMove = Invoke-SideWaypoint -destination $iron
            $iron = Get-NearestVein 'Iron'
        }

        $ironHarvest = Invoke-Harvest -nodeId ([int]$iron.nodeId) -count $ironDeficit
    }

    if (-not $electromagnetismState.unlocked) {
        $player = Get-PlayerSnapshot
        $coilDeficit = [Math]::Max(0, 10 - (Get-InventoryCount $player '磁线圈'))
        if ($coilDeficit -gt 0) {
            $firstCoils = Invoke-HandcraftOutput -outputName '磁线圈' -outputCount $coilDeficit
        }

        $electromagnetism = Complete-Technology -technologyName '电磁学'
    }

    $player = Get-PlayerSnapshot
    $coilDeficit = [Math]::Max(0, 10 - (Get-InventoryCount $player '磁线圈'))
    if ($coilDeficit -gt 0) {
        $secondCoils = Invoke-HandcraftOutput -outputName '磁线圈' -outputCount $coilDeficit
    }

    $player = Get-PlayerSnapshot
    $boardDeficit = [Math]::Max(0, 10 - (Get-InventoryCount $player '电路板'))
    if ($boardDeficit -gt 0) {
        $boards = Invoke-HandcraftOutput -outputName '电路板' -outputCount $boardDeficit
    }

    $blueMatrixTechnology = Complete-Technology -technologyName '电磁矩阵'
}

[pscustomobject]@{
    sessionId = $sessionId
    planetId = $planetId
    copperHarvest = $(if ($null -eq $copperHarvest) { $null } else { $copperHarvest.result })
    waypointMove = $(if ($null -eq $waypointMove) { $null } else { $waypointMove.result })
    ironHarvest = $(if ($null -eq $ironHarvest) { $null } else { $ironHarvest.result })
    firstCoils = $(if ($null -eq $firstCoils) { $null } else { $firstCoils.result })
    electromagnetism = $electromagnetism
    secondCoils = $(if ($null -eq $secondCoils) { $null } else { $secondCoils.result })
    boards = $(if ($null -eq $boards) { $null } else { $boards.result })
    blueMatrixTechnology = $blueMatrixTechnology
    finalPlayer = Get-PlayerSnapshot
    finalProgression = Get-ProgressionSnapshot
} | ConvertTo-Json -Depth 16
