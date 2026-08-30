[CmdletBinding()]
param(
    [string]$DspDir,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (Get-Process -Name 'DSPGAME' -ErrorAction SilentlyContinue) {
    throw 'DSPGAME is running. Exit the game before installing the development plugin.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$locator = Join-Path $PSScriptRoot 'locate-dsp.ps1'
$locationJson = if ($DspDir) {
    & $locator -DspDir $DspDir -AsJson
} else {
    & $locator -AsJson
}
$location = $locationJson | ConvertFrom-Json
$gameRoot = [IO.Path]::GetFullPath([string]$location.path)
$bepInEx = Join-Path $gameRoot 'BepInEx\core\BepInEx.dll'
if (-not (Test-Path -LiteralPath $bepInEx)) {
    throw 'BepInEx is not installed in the selected DSP directory.'
}

if (-not $NoBuild) {
    & (Join-Path $PSScriptRoot 'sync-game-refs.ps1') -DspDir $gameRoot | Out-Null
    dotnet build (Join-Path $repoRoot 'Spherewright.sln') -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw 'Spherewright solution build failed.'
    }
}

$outputDirectory = Join-Path $repoRoot "src\Spherewright.Plugin\bin\$Configuration\net472"
$destination = Join-Path $gameRoot 'BepInEx\plugins\Spherewright'
New-Item -ItemType Directory -Path $destination -Force | Out-Null

$requiredFiles = @(
    'Spherewright.Plugin.dll',
    'Spherewright.Contracts.dll',
    'Spherewright.Bridge.Core.dll',
    'Newtonsoft.Json.dll'
)

foreach ($name in $requiredFiles) {
    $source = Join-Path $outputDirectory $name
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Expected plugin output is missing: $source"
    }

    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $name) -Force
}

Get-ChildItem -LiteralPath $outputDirectory -Filter 'Spherewright.*.pdb' -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $destination $_.Name) -Force
}

[pscustomobject]@{
    installedTo = $destination
    configuration = $Configuration
    files = $requiredFiles
} | ConvertTo-Json -Depth 3

