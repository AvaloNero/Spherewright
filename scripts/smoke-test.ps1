[CmdletBinding()]
param([switch]$LiveBridge)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionFilter = Join-Path $repoRoot 'Spherewright.Core.slnf'

dotnet restore $solutionFilter --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }
dotnet build $solutionFilter --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Core build failed.' }
dotnet test $solutionFilter --no-build
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }

if (-not $LiveBridge) {
    return
}

$runtimeDirectory = Join-Path $env:LOCALAPPDATA 'Spherewright\runtime'
$descriptors = @(Get-ChildItem -LiteralPath $runtimeDirectory -Filter 'bridge-*.json' -File -ErrorAction Stop)
if ($descriptors.Count -ne 1) {
    throw "Expected exactly one live bridge descriptor, found $($descriptors.Count)."
}

$descriptorFile = $descriptors[0]
$descriptor = Get-Content -LiteralPath $descriptorFile.FullName -Raw | ConvertFrom-Json
$process = Get-Process -Id ([int]$descriptor.processId) -ErrorAction Stop
if ($process.ProcessName -ne 'DSPGAME') {
    throw 'Descriptor PID does not identify DSPGAME.'
}

$currentSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$acl = Get-Acl -LiteralPath $descriptorFile.FullName
$ownerSid = ([Security.Principal.NTAccount]$acl.Owner).Translate([Security.Principal.SecurityIdentifier]).Value
if ($ownerSid -ne $currentSid) {
    throw 'Bridge descriptor is not owned by the current user SID.'
}

function Write-BridgeFrame {
    param(
        [Parameter(Mandatory)][IO.Stream]$Stream,
        [Parameter(Mandatory)][object]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 10 -Compress
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

$badPipe = [IO.Pipes.NamedPipeClientStream]::new(
    '.',
    [string]$descriptor.pipeName,
    [IO.Pipes.PipeDirection]::InOut,
    [IO.Pipes.PipeOptions]::Asynchronous)
try {
    $badPipe.Connect(5000)
    Write-BridgeFrame -Stream $badPipe -Value @{
        protocolVersion = 1
        messageType = 'handshake'
        requestId = [guid]::NewGuid().ToString('D')
        payload = @{
            bridgeInstanceId = [string]$descriptor.bridgeInstanceId
            authToken = 'intentionally-invalid-token'
            clientName = 'Spherewright.SmokeTest'
            clientVersion = '0.1.0'
        }
    }

    $probeBuffer = [byte[]]::new(1)
    $probeCancellation = [Threading.CancellationTokenSource]::new(3000)
    try {
        $read = $badPipe.ReadAsync($probeBuffer, 0, 1, $probeCancellation.Token).GetAwaiter().GetResult()
    } finally {
        $probeCancellation.Dispose()
    }

    if ($read -ne 0) {
        throw 'A bridge response was returned for an invalid authentication token.'
    }
} finally {
    $badPipe.Dispose()
}

$pipe = [IO.Pipes.NamedPipeClientStream]::new(
    '.',
    [string]$descriptor.pipeName,
    [IO.Pipes.PipeDirection]::InOut,
    [IO.Pipes.PipeOptions]::None)
try {
    $pipe.Connect(5000)
    $handshakeId = [guid]::NewGuid().ToString('D')
    Write-BridgeFrame -Stream $pipe -Value @{
        protocolVersion = 1
        messageType = 'handshake'
        requestId = $handshakeId
        payload = @{
            bridgeInstanceId = [string]$descriptor.bridgeInstanceId
            authToken = [string]$descriptor.authToken
            clientName = 'Spherewright.SmokeTest'
            clientVersion = '0.1.0'
        }
    }
    $handshake = Read-BridgeFrame -Stream $pipe
    if (-not $handshake.success) { throw 'Bridge handshake was rejected.' }

    Write-BridgeFrame -Stream $pipe -Value @{
        protocolVersion = 1
        messageType = 'request'
        requestId = [guid]::NewGuid().ToString('D')
        method = 'get_bridge_status'
        payload = @{}
    }
    $status = Read-BridgeFrame -Stream $pipe
    if (-not $status.success -or -not $status.result.bridgeConnected) {
        throw 'Bridge status request failed.'
    }

    [ordered]@{
        wrongTokenRejected = $true
        status = $status.result
    } | ConvertTo-Json -Depth 5
} finally {
    $pipe.Dispose()
}
