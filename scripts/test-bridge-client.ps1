[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpherewrightBridgeClient.ps1')
$swPassed = 0
function Assert-SwClient([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw "Bridge client regression: $Label" }
    $script:swPassed++
}
function Assert-SwReadFails([byte[]]$Bytes, [string]$Label) {
    $swStream = [IO.MemoryStream]::new($Bytes, $false)
    $swThrew = $false
    try { $null = Read-SpherewrightBridgeFrame -Stream $swStream }
    catch { $swThrew = $true }
    finally { $swStream.Dispose() }
    Assert-SwClient $swThrew $Label
}

$swStream = [IO.MemoryStream]::new([byte[]]@(0, 1, 128, 255), $false)
try {
    $swBytes = Read-SpherewrightExactBytes -Stream $swStream -Count 4
    Assert-SwClient ($swBytes -is [byte[]]) 'read returns one byte array, not boxed pipeline elements'
    Assert-SwClient (($swBytes -join ',') -eq '0,1,128,255') 'all byte values retained'
    $swEmpty = Read-SpherewrightExactBytes -Stream $swStream -Count 0
    Assert-SwClient (($swEmpty -is [byte[]]) -and $swEmpty.Length -eq 0) 'empty read retains byte-array identity'
} finally { $swStream.Dispose() }

$swStream = [IO.MemoryStream]::new()
try {
    Write-SpherewrightBridgeFrame -Stream $swStream -Value @{
        success = $true
        result = @{ label = '铁块 / Icarus'; values = @(1, 2, 3); padding = ('x' * 262144) }
    }
    $swStream.Position = 0
    $swResponse = Read-SpherewrightBridgeFrame -Stream $swStream
    Assert-SwClient ($swResponse.success -and $swResponse.result.label -eq '铁块 / Icarus' -and
        ($swResponse.result.values -join ',') -eq '1,2,3' -and $swResponse.result.padding.Length -eq 262144) 'large UTF-8 frame roundtrip'
    Assert-SwClient ($swStream.Position -eq $swStream.Length) 'frame consumed exactly'
} finally { $swStream.Dispose() }

Assert-SwReadFails ([BitConverter]::GetBytes(-1)) 'negative frame length rejects'
Assert-SwReadFails ([BitConverter]::GetBytes(1048577)) 'oversized frame rejects before payload'
Assert-SwReadFails ([byte[]]@(1, 0)) 'truncated header rejects'
Assert-SwReadFails ([byte[]](@(10, 0, 0, 0) + @(123))) 'truncated payload rejects'

[pscustomobject]@{ passed = $swPassed; failed = 0; gameRequests = 0 } | ConvertTo-Json
