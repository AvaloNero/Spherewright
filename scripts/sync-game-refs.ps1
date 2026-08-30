[CmdletBinding()]
param([string]$DspDir)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$locator = Join-Path $PSScriptRoot 'locate-dsp.ps1'
$locationJson = if ($DspDir) {
    & $locator -DspDir $DspDir -AsJson
} else {
    & $locator -AsJson
}
$location = $locationJson | ConvertFrom-Json
$gameRoot = [IO.Path]::GetFullPath([string]$location.path)

$sources = [ordered]@{
    'Assembly-CSharp.dll' = Join-Path $gameRoot 'DSPGAME_Data\Managed\Assembly-CSharp.dll'
    'UnityEngine.dll' = Join-Path $gameRoot 'DSPGAME_Data\Managed\UnityEngine.dll'
    'UnityEngine.CoreModule.dll' = Join-Path $gameRoot 'DSPGAME_Data\Managed\UnityEngine.CoreModule.dll'
    'BepInEx.dll' = Join-Path $gameRoot 'BepInEx\core\BepInEx.dll'
}

foreach ($entry in $sources.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $entry.Value)) {
        throw "Required compile reference is missing: $($entry.Value)"
    }
}

$destination = Join-Path $repoRoot '.local\game-refs'
New-Item -ItemType Directory -Path $destination -Force | Out-Null

$manifestEntries = @()
foreach ($entry in $sources.GetEnumerator()) {
    $target = Join-Path $destination $entry.Key
    Copy-Item -LiteralPath $entry.Value -Destination $target -Force
    $file = Get-Item -LiteralPath $target
    $manifestEntries += [pscustomobject]@{
        name = $entry.Key
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    }
}

$manifest = [pscustomobject]@{
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    gameRootSource = [string]$location.source
    gameRoot = $gameRoot
    files = $manifestEntries
}

$manifestPath = Join-Path $destination 'manifest.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$manifest | ConvertTo-Json -Depth 5

