[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$SourceObjectId,
    [Parameter(Mandatory)][int]$DestinationObjectId,
    [double[]]$CandidateDistances = @(3, 4, 5, 6, 8, 10),
    [double[]]$CandidateAngles = @(-90, -75, -60, -45, -30, -15, 0, 15, 30, 45, 60, 75, 90),
    [ValidateRange(1, 96)][int]$PrepareBatchSize = 64,
    [switch]$KeepPlansActive,
    [ValidateRange(1, 100)][int]$Limit = 25
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')

function Get-Magnitude([double[]]$Vector) {
    return [Math]::Sqrt(
        $Vector[0] * $Vector[0] +
        $Vector[1] * $Vector[1] +
        $Vector[2] * $Vector[2])
}

function Get-Distance([object]$Left, [object]$Right) {
    $x = [double]$Left.x - [double]$Right.x
    $y = [double]$Left.y - [double]$Right.y
    $z = [double]$Left.z - [double]$Right.z
    return [Math]::Sqrt($x * $x + $y * $y + $z * $z)
}

function Wait-ForPreparedPlansToExpire([Collections.Generic.List[DateTimeOffset]]$Expirations) {
    if ($Expirations.Count -eq 0) {
        return
    }

    $latest = ($Expirations | Sort-Object -Descending | Select-Object -First 1).AddMilliseconds(250)
    while ([DateTimeOffset]::UtcNow -lt $latest) {
        $remaining = [Math]::Ceiling(($latest - [DateTimeOffset]::UtcNow).TotalMilliseconds)
        Start-Sleep -Milliseconds ([Math]::Min(1000, [Math]::Max(50, $remaining)))
    }

    $Expirations.Clear()
}

$session = Get-SpherewrightOwnedSession
$sessionId = [string]$session.sessionId
$planetId = [int]$session.localPlanetId
$player = Get-SpherewrightBridgeResult -Response (
    Invoke-SpherewrightBridgeRequest -Method 'get_player_state' -SessionId $sessionId -Payload @{
        planetId = $planetId
    }) -Operation 'get_player_state'
$source = Get-SpherewrightBridgeResult -Response (
    Invoke-SpherewrightBridgeRequest -Method 'inspect_factory_entity' -SessionId $sessionId -Payload @{
        planetId = $planetId
        objectId = $SourceObjectId
    }) -Operation 'inspect_factory_entity(source)'
$destination = Get-SpherewrightBridgeResult -Response (
    Invoke-SpherewrightBridgeRequest -Method 'inspect_factory_entity' -SessionId $sessionId -Payload @{
        planetId = $planetId
        objectId = $DestinationObjectId
    }) -Operation 'inspect_factory_entity(destination)'

if ($source.componentKind -ne 'belt') {
    throw "Source object $SourceObjectId is not a completed belt."
}

$sourceOutputs = @($source.connections | Where-Object { $_.isOutput })
if ($sourceOutputs.Count -ne 0) {
    throw "Source belt $SourceObjectId is not a free output endpoint."
}

$entities = @()
$cursor = ''
do {
    $payload = @{ planetId = $planetId; limit = 100 }
    if ($cursor) {
        $payload.cursor = $cursor
    }

    $page = Get-SpherewrightBridgeResult -Response (
        Invoke-SpherewrightBridgeRequest -Method 'list_factory_entities' -SessionId $sessionId -Payload $payload
    ) -Operation 'list_factory_entities'
    $entities += @($page.entities)
    $cursor = [string]$page.nextCursor
} while ($cursor)

$clearanceEntities = @($entities | Where-Object {
    $_.objectKind -eq 'entity' -and $_.componentKind -notin @('belt', 'inserter')
})
$existingBelts = @($entities | Where-Object {
    $_.objectKind -eq 'entity' -and
    $_.componentKind -eq 'belt' -and
    [int]$_.objectId -ne $SourceObjectId
})

[double[]]$sourceVector = @(
    [double]$source.position.x,
    [double]$source.position.y,
    [double]$source.position.z)
[double[]]$targetVector = @(
    [double]$destination.position.x,
    [double]$destination.position.y,
    [double]$destination.position.z)
$radius = Get-Magnitude $sourceVector
$targetRadius = Get-Magnitude $targetVector
if ($radius -lt 1 -or $targetRadius -lt 1) {
    throw 'Source or destination position is not a valid planet-surface vector.'
}

[double[]]$up = @(
    ($sourceVector[0] / $radius),
    ($sourceVector[1] / $radius),
    ($sourceVector[2] / $radius))
[double[]]$targetUp = @(
    ($targetVector[0] / $targetRadius),
    ($targetVector[1] / $targetRadius),
    ($targetVector[2] / $targetRadius))
$dot = $up[0] * $targetUp[0] + $up[1] * $targetUp[1] + $up[2] * $targetUp[2]
[double[]]$forward = @(
    ($targetUp[0] - $dot * $up[0]),
    ($targetUp[1] - $dot * $up[1]),
    ($targetUp[2] - $dot * $up[2]))
$forwardMagnitude = Get-Magnitude $forward
if ($forwardMagnitude -lt 0.000001) {
    throw 'Source and destination do not define a stable tangent direction.'
}

$forward = @(
    ($forward[0] / $forwardMagnitude),
    ($forward[1] / $forwardMagnitude),
    ($forward[2] / $forwardMagnitude))
[double[]]$side = @(
    ($up[1] * $forward[2] - $up[2] * $forward[1]),
    ($up[2] * $forward[0] - $up[0] * $forward[2]),
    ($up[0] * $forward[1] - $up[1] * $forward[0]))
$sourceDestinationDistance = Get-Distance $source.position $destination.position

$results = @()
$activePlanExpirations = [Collections.Generic.List[DateTimeOffset]]::new()
$attemptsInBatch = 0
$overlapRejectionCount = 0
$prepareRejectionCounts = @{}
foreach ($distance in $CandidateDistances) {
    if ($distance -le 0) {
        continue
    }

    foreach ($angle in $CandidateAngles) {
        if ($attemptsInBatch -ge $PrepareBatchSize) {
            Wait-ForPreparedPlansToExpire $activePlanExpirations
            $attemptsInBatch = 0
            $player = Get-SpherewrightBridgeResult -Response (
                Invoke-SpherewrightBridgeRequest -Method 'get_player_state' -SessionId $sessionId -Payload @{
                    planetId = $planetId
                }) -Operation 'get_player_state(batch refresh)'
            $source = Get-SpherewrightBridgeResult -Response (
                Invoke-SpherewrightBridgeRequest -Method 'inspect_factory_entity' -SessionId $sessionId -Payload @{
                    planetId = $planetId
                    objectId = $SourceObjectId
                }) -Operation 'inspect_factory_entity(batch refresh)'
        }

        $angleRadians = $angle * [Math]::PI / 180
        [double[]]$direction = @(
            ([Math]::Cos($angleRadians) * $forward[0] + [Math]::Sin($angleRadians) * $side[0]),
            ([Math]::Cos($angleRadians) * $forward[1] + [Math]::Sin($angleRadians) * $side[1]),
            ([Math]::Cos($angleRadians) * $forward[2] + [Math]::Sin($angleRadians) * $side[2]))
        $surfaceAngle = $distance / $radius
        $candidateEnd = @{
            x = $radius * ([Math]::Cos($surfaceAngle) * $up[0] + [Math]::Sin($surfaceAngle) * $direction[0])
            y = $radius * ([Math]::Cos($surfaceAngle) * $up[1] + [Math]::Sin($surfaceAngle) * $direction[1])
            z = $radius * ([Math]::Cos($surfaceAngle) * $up[2] + [Math]::Sin($surfaceAngle) * $direction[2])
        }

        try {
            # Prepare is intentionally the only game call in this loop. It creates no prebuild
            # and consumes no item; callers must fresh-read before any separate commit.
            $prepared = Get-SpherewrightBridgeResult -Response (
                Invoke-SpherewrightBridgeRequest -Method 'prepare_build' -SessionId $sessionId -Payload @{
                    planetId = $planetId
                    buildingItemId = 2001
                    sourceObjectId = $SourceObjectId
                    expectedSourceStateHash = [string]$source.endpointStateHash
                    pathEnd = $candidateEnd
                    expectedPlayerStateHash = [string]$player.stateHash
                    stateHashVersion = 1
                }) -Operation 'prepare_build'
            $attemptsInBatch++
            $activePlanExpirations.Add([DateTimeOffset]::Parse([string]$prepared.expiresAtUtc))

            $plannedPath = @($prepared.plannedPath)
            $overlappingBelt = $null
            for ($pathIndex = 1; $pathIndex -lt $plannedPath.Count -and $null -eq $overlappingBelt; $pathIndex++) {
                foreach ($belt in $existingBelts) {
                    if ((Get-Distance $plannedPath[$pathIndex] $belt.position) -lt 0.25) {
                        $overlappingBelt = $belt
                        break
                    }
                }
            }

            if ($null -ne $overlappingBelt) {
                $overlapRejectionCount++
                Write-Verbose "Rejected distance=$distance angle=$angle because the snapped path overlaps existing belt $($overlappingBelt.objectId)."
                continue
            }

            $minimumClearance = [double]::PositiveInfinity
            $nearest = $null
            $minimumNewClearance = [double]::PositiveInfinity
            $nearestNew = $null
            for ($pathIndex = 0; $pathIndex -lt $plannedPath.Count; $pathIndex++) {
                $point = $plannedPath[$pathIndex]
                foreach ($entity in $clearanceEntities) {
                    $clearance = Get-Distance $point $entity.position
                    if ($clearance -lt $minimumClearance) {
                        $minimumClearance = $clearance
                        $nearest = $entity
                    }

                    if ($pathIndex -gt 0 -and $clearance -lt $minimumNewClearance) {
                        $minimumNewClearance = $clearance
                        $nearestNew = $entity
                    }
                }
            }

            $plannedEnd = $plannedPath[-1]
            $destinationDistance = Get-Distance $plannedEnd $destination.position
            $results += [pscustomobject]@{
                requestedDistance = $distance
                requestedAngle = $angle
                beltCount = $plannedPath.Count
                minimumEntityCenterClearance = [Math]::Round($minimumClearance, 3)
                nearestObjectId = [int]$nearest.objectId
                nearestComponentKind = [string]$nearest.componentKind
                minimumNewEntityCenterClearance = [Math]::Round($minimumNewClearance, 3)
                nearestNewObjectId = [int]$nearestNew.objectId
                nearestNewComponentKind = [string]$nearestNew.componentKind
                destinationDistance = [Math]::Round($destinationDistance, 3)
                destinationProgress = [Math]::Round(($sourceDestinationDistance - $destinationDistance), 3)
                playerDistance = [Math]::Round((Get-Distance $plannedEnd $player.position), 3)
                plannedEnd = $plannedEnd
            }
        } catch {
            $attemptsInBatch++
            $message = [string]$_.Exception.Message
            $reason = if ($message -match 'failed:\s+([A-Z][A-Z0-9_]+):') {
                $Matches[1]
            } else {
                $_.Exception.GetType().Name
            }
            if ($prepareRejectionCounts.ContainsKey($reason)) {
                $prepareRejectionCounts[$reason]++
            } else {
                $prepareRejectionCounts[$reason] = 1
            }
            Write-Verbose "Rejected distance=$distance angle=${angle}: $($_.Exception.Message)"
        }
    }
}

if (-not $KeepPlansActive) {
    Wait-ForPreparedPlansToExpire $activePlanExpirations
}

if ($results.Count -eq 0) {
    $prepareSummary = if ($prepareRejectionCounts.Count -eq 0) {
        'none'
    } else {
        (@($prepareRejectionCounts.GetEnumerator() |
            Sort-Object Name |
            ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ', ')
    }
    Write-Warning "No zero-overlap belt route candidate remained (overlap=$overlapRejectionCount; prepare=$prepareSummary)."
}

$results |
    Sort-Object `
        @{ Expression = 'minimumNewEntityCenterClearance'; Descending = $true },
        @{ Expression = 'destinationProgress'; Descending = $true } |
    Select-Object -First $Limit
