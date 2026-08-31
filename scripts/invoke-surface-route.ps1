[CmdletBinding()]
param(
    [Parameter(Mandatory)][double]$DestinationX,
    [Parameter(Mandatory)][double]$DestinationY,
    [Parameter(Mandatory)][double]$DestinationZ,
    [ValidateRange(10, 50)][double]$MaximumSegmentLength = 30,
    [ValidateRange(0.5, 5)][double]$ArrivalTolerance = 2.5,
    [ValidateRange(1, 1800)][int]$ActionTimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')

function Get-CurrentPlayerSnapshot {
    return Get-SpherewrightBridgeResult -Response (
        Invoke-SpherewrightBridgeRequest -Method 'get_player_state' -SessionId $script:SessionId -Payload @{
            planetId = $script:PlanetId
        }) -Operation 'get_player_state'
}

function Get-VectorMagnitude([double[]]$Vector) {
    $squaredMagnitude = $Vector[0] * $Vector[0] + $Vector[1] * $Vector[1] + $Vector[2] * $Vector[2]
    return [Math]::Sqrt($squaredMagnitude)
}

function Wait-ForSettledPlayer([int]$Seconds = 10) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    do {
        $snapshot = Get-CurrentPlayerSnapshot
        if ($snapshot.movementState -eq 'Walk' -and [double]$snapshot.speed -le 0.1) {
            return $snapshot
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    throw "The player did not settle into Walk state below 0.1 m/s after the settle window (state=$($snapshot.movementState), speed=$($snapshot.speed))."
}

function Get-SurfacePoint(
    [double[]]$StartUnit,
    [double[]]$EndUnit,
    [double]$Angle,
    [double]$Radius,
    [double]$Fraction) {
    $sinAngle = [Math]::Sin($Angle)
    if ([Math]::Abs($sinAngle) -lt 0.00000001) {
        $mixed = @(
            (1.0 - $Fraction) * $StartUnit[0] + $Fraction * $EndUnit[0],
            (1.0 - $Fraction) * $StartUnit[1] + $Fraction * $EndUnit[1],
            (1.0 - $Fraction) * $StartUnit[2] + $Fraction * $EndUnit[2])
        $mixedMagnitude = Get-VectorMagnitude $mixed
        if ($mixedMagnitude -lt 0.00000001) {
            throw 'A near-antipodal surface route is ambiguous; split it through an explicit side waypoint.'
        }

        return @(
            ($mixed[0] / $mixedMagnitude * $Radius),
            ($mixed[1] / $mixedMagnitude * $Radius),
            ($mixed[2] / $mixedMagnitude * $Radius))
    }

    $startWeight = [Math]::Sin((1.0 - $Fraction) * $Angle) / $sinAngle
    $endWeight = [Math]::Sin($Fraction * $Angle) / $sinAngle
    return @(
        (($startWeight * $StartUnit[0] + $endWeight * $EndUnit[0]) * $Radius),
        (($startWeight * $StartUnit[1] + $endWeight * $EndUnit[1]) * $Radius),
        (($startWeight * $StartUnit[2] + $endWeight * $EndUnit[2]) * $Radius))
}

$session = Get-SpherewrightOwnedSession
$script:SessionId = [string]$session.sessionId
$script:PlanetId = [int]$session.localPlanetId
$player = Get-CurrentPlayerSnapshot
if (-not $player.isAlive -or -not $player.isOnPlanet -or $player.movementState -ne 'Walk') {
    throw 'Surface routing requires a living, grounded player in Walk state.'
}

$start = @(
    [double]$player.position.x,
    [double]$player.position.y,
    [double]$player.position.z)
$destination = @($DestinationX, $DestinationY, $DestinationZ)
$radius = Get-VectorMagnitude $start
$destinationRadius = Get-VectorMagnitude $destination
if ($radius -lt 1.0 -or $destinationRadius -lt 1.0 -or [Math]::Abs($destinationRadius - $radius) -gt 8.0) {
    throw 'The destination is not on the current local-planet surface.'
}

$startUnit = @(($start[0] / $radius), ($start[1] / $radius), ($start[2] / $radius))
$endUnit = @(
    ($destination[0] / $destinationRadius),
    ($destination[1] / $destinationRadius),
    ($destination[2] / $destinationRadius))
[double]$dot = $startUnit[0] * $endUnit[0] + $startUnit[1] * $endUnit[1] + $startUnit[2] * $endUnit[2]
if ($dot -gt 1.0) {
    $dot = 1.0
} elseif ($dot -lt -1.0) {
    $dot = -1.0
}

$angle = [Math]::Acos($dot)
$arcLength = $angle * $radius
$segmentCount = [Math]::Max(1, [int][Math]::Ceiling($arcLength / $MaximumSegmentLength))

Write-Output ("SURFACE_ROUTE_STARTED planet={0} arcMeters={1:N1} segments={2}" -f $script:PlanetId, $arcLength, $segmentCount)
for ($step = 1; $step -le $segmentCount; $step++) {
    $fraction = $step / [double]$segmentCount
    $target = Get-SurfacePoint $startUnit $endUnit $angle $radius $fraction
    $action = $null
    for ($prepareAttempt = 1; $prepareAttempt -le 20; $prepareAttempt++) {
        $player = Get-CurrentPlayerSnapshot
        try {
            $action = Invoke-SpherewrightNormalAction `
                -PrepareMethod 'prepare_move' `
                -CommitMethod 'commit_move' `
                -SessionId $script:SessionId `
                -PlanetId $script:PlanetId `
                -TimeoutSeconds $ActionTimeoutSeconds `
                -PreparePayload @{
                    planetId = $script:PlanetId
                    target = @{ x = $target[0]; y = $target[1]; z = $target[2] }
                    arrivalTolerance = $ArrivalTolerance
                    expectedPlayerStateHash = [string]$player.stateHash
                    stateHashVersion = 1
                }
            break
        } catch {
            if ($prepareAttempt -ge 20 -or $_.Exception.Message -notlike 'prepare_move failed: STALE_STATE:*') {
                throw
            }
        }
    }

    if ($null -eq $action) {
        throw "Surface route step $step did not obtain a fresh movement plan."
    }

    $after = Wait-ForSettledPlayer
    $dx = [double]$after.position.x - $target[0]
    $dy = [double]$after.position.y - $target[1]
    $dz = [double]$after.position.z - $target[2]
    $remaining = [Math]::Sqrt($dx * $dx + $dy * $dy + $dz * $dz)
    Write-Output ("SURFACE_ROUTE_STEP step={0}/{1} action={2} targetErrorMeters={3:N2} coreMJ={4:N1}" -f `
        $step,
        $segmentCount,
        $action.committed.actionId,
        $remaining,
        ([double]$after.coreEnergy / 1000000.0))
}

$final = Wait-ForSettledPlayer
$finalDestination = @(
    ($endUnit[0] * $radius),
    ($endUnit[1] * $radius),
    ($endUnit[2] * $radius))
$finalDx = [double]$final.position.x - $finalDestination[0]
$finalDy = [double]$final.position.y - $finalDestination[1]
$finalDz = [double]$final.position.z - $finalDestination[2]
$finalSquaredDistance = $finalDx * $finalDx + $finalDy * $finalDy + $finalDz * $finalDz
$finalDistance = [Math]::Sqrt($finalSquaredDistance)

[pscustomobject]@{
    completed = $true
    planetId = $script:PlanetId
    segmentCount = $segmentCount
    surfaceArcMeters = $arcLength
    finalDistanceMeters = $finalDistance
    movementState = $final.movementState
    speed = $final.speed
    coreEnergy = $final.coreEnergy
    position = $final.position
}
