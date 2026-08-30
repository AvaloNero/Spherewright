Set-StrictMode -Version Latest

function Get-LiveSpherewrightDescriptor {
    [CmdletBinding()]
    param(
        [ValidateRange(1, 120)][int]$TimeoutSeconds = 30
    )

    $runtimeDirectory = Join-Path $env:LOCALAPPDATA 'Spherewright\runtime'
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
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
        if ($descriptors.Count -eq 1) {
            return Get-Content -LiteralPath $descriptors[0].FullName -Raw | ConvertFrom-Json
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    throw "Expected exactly one live Spherewright bridge descriptor, found $($descriptors.Count)."
}

function Write-SpherewrightBridgeFrame {
    param(
        [Parameter(Mandatory)][IO.Stream]$Stream,
        [Parameter(Mandatory)][object]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 30 -Compress
    $payload = [Text.Encoding]::UTF8.GetBytes($json)
    $header = [BitConverter]::GetBytes([int]$payload.Length)
    $Stream.Write($header, 0, $header.Length)
    $Stream.Write($payload, 0, $payload.Length)
    $Stream.Flush()
}

function Read-SpherewrightExactBytes {
    param(
        [Parameter(Mandatory)][IO.Stream]$Stream,
        [Parameter(Mandatory)][int]$Count
    )

    $buffer = [byte[]]::new($Count)
    $offset = 0
    while ($offset -lt $Count) {
        $read = $Stream.Read($buffer, $offset, $Count - $offset)
        if ($read -eq 0) {
            throw 'Spherewright bridge connection closed during a frame.'
        }

        $offset += $read
    }

    return $buffer
}

function Read-SpherewrightBridgeFrame {
    param([Parameter(Mandatory)][IO.Stream]$Stream)

    $header = Read-SpherewrightExactBytes -Stream $Stream -Count 4
    $length = [BitConverter]::ToInt32($header, 0)
    if ($length -lt 0 -or $length -gt 1048576) {
        throw "Invalid Spherewright bridge frame length: $length"
    }

    $payload = Read-SpherewrightExactBytes -Stream $Stream -Count $length
    return ([Text.Encoding]::UTF8.GetString($payload) | ConvertFrom-Json)
}

function Invoke-SpherewrightBridgeRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][object]$Payload,
        [string]$SessionId,
        [object]$Descriptor = (Get-LiveSpherewrightDescriptor)
    )

    $pipe = [IO.Pipes.NamedPipeClientStream]::new(
        '.',
        [string]$Descriptor.pipeName,
        [IO.Pipes.PipeDirection]::InOut,
        [IO.Pipes.PipeOptions]::None)
    try {
        $pipe.Connect(10000)
        $handshakeId = [guid]::NewGuid().ToString('D')
        Write-SpherewrightBridgeFrame -Stream $pipe -Value @{
            protocolVersion = 1
            messageType = 'handshake'
            requestId = $handshakeId
            payload = @{
                bridgeInstanceId = [string]$Descriptor.bridgeInstanceId
                authToken = [string]$Descriptor.authToken
                clientName = 'Spherewright.LocalVerification'
                clientVersion = '0.1.0'
            }
        }
        $handshake = Read-SpherewrightBridgeFrame -Stream $pipe
        if (-not $handshake.success) {
            throw 'Spherewright bridge handshake failed.'
        }

        $request = @{
            protocolVersion = 1
            messageType = 'request'
            requestId = [guid]::NewGuid().ToString('D')
            method = $Method
            payload = $Payload
        }
        if ($SessionId) {
            $request.sessionId = $SessionId
        }

        Write-SpherewrightBridgeFrame -Stream $pipe -Value $request
        return Read-SpherewrightBridgeFrame -Stream $pipe
    } finally {
        $pipe.Dispose()
    }
}

function Get-SpherewrightBridgeResult {
    param(
        [Parameter(Mandatory)][object]$Response,
        [Parameter(Mandatory)][string]$Operation
    )

    if (-not $Response.success) {
        throw "$Operation failed: $($Response.error.code): $($Response.error.message)"
    }

    return $Response.result
}
