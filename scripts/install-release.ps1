[CmdletBinding()]
param(
    [string]$DspDir,
    [string]$McpDestination,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (Get-Process -Name 'DSPGAME' -ErrorAction SilentlyContinue) {
    throw 'DSPGAME is running. Exit the game before installing Spherewright.'
}

$packageRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$manifestPath = Join-Path $packageRoot 'manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'manifest.json is missing from the Spherewright release package.'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ([int]$manifest.schemaVersion -ne 1 -or [string]$manifest.package -ne 'Spherewright') {
    throw 'The release manifest is not a supported Spherewright package.'
}

$version = [string]$manifest.version
if ($version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
    throw 'The release manifest contains an invalid version.'
}

$packagePrefix = $packageRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
foreach ($entry in @($manifest.files)) {
    $relativePath = [string]$entry.path
    if ([string]::IsNullOrWhiteSpace($relativePath) -or [IO.Path]::IsPathRooted($relativePath)) {
        throw 'The release manifest contains an unsafe file path.'
    }

    $candidate = [IO.Path]::GetFullPath((Join-Path $packageRoot ($relativePath -replace '/', [IO.Path]::DirectorySeparatorChar)))
    if (-not $candidate.StartsWith($packagePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The release manifest contains a path outside the package.'
    }

    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "A release file is missing: $relativePath"
    }

    $actualHash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash
    if (-not [string]::Equals($actualHash, [string]$entry.sha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Release integrity verification failed: $relativePath"
    }
}

$locator = Join-Path $packageRoot 'locate-dsp.ps1'
$locationJson = if ($DspDir) {
    & $locator -DspDir $DspDir -AsJson
} else {
    & $locator -AsJson
}
$location = $locationJson | ConvertFrom-Json
$gameRoot = [IO.Path]::GetFullPath([string]$location.path)
$bepInEx = Join-Path $gameRoot 'BepInEx\core\BepInEx.dll'
if (-not (Test-Path -LiteralPath $bepInEx -PathType Leaf)) {
    throw 'BepInEx 5 is not installed in the selected DSP directory.'
}

$pluginSource = Join-Path $packageRoot 'BepInEx\plugins\Spherewright'
$pluginDestination = Join-Path $gameRoot 'BepInEx\plugins\Spherewright'
if (-not (Test-Path -LiteralPath $pluginSource -PathType Container)) {
    throw 'The packaged Plugin directory is missing.'
}

if (-not $McpDestination) {
    $McpDestination = Join-Path $env:LOCALAPPDATA "Spherewright\mcp\$version"
}
$resolvedMcpDestination = [IO.Path]::GetFullPath($McpDestination)
if ((Test-Path -LiteralPath $resolvedMcpDestination -PathType Container) -and
    @(Get-ChildItem -LiteralPath $resolvedMcpDestination -Force).Count -gt 0 -and
    -not $Force) {
    throw "The MCP destination already contains files. Re-run with -Force to reinstall this version: $resolvedMcpDestination"
}

New-Item -ItemType Directory -Path $pluginDestination -Force | Out-Null
foreach ($file in @(Get-ChildItem -LiteralPath $pluginSource -File)) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $pluginDestination $file.Name) -Force
}

$mcpSource = Join-Path $packageRoot 'mcp'
if (-not (Test-Path -LiteralPath (Join-Path $mcpSource 'Spherewright.Mcp.exe') -PathType Leaf)) {
    throw 'The packaged self-contained MCP executable is missing.'
}
New-Item -ItemType Directory -Path $resolvedMcpDestination -Force | Out-Null
foreach ($entry in @(Get-ChildItem -LiteralPath $mcpSource -Force)) {
    Copy-Item -LiteralPath $entry.FullName -Destination $resolvedMcpDestination -Recurse -Force
}

$mcpExecutable = Join-Path $resolvedMcpDestination 'Spherewright.Mcp.exe'
[pscustomobject]@{
    version = $version
    pluginInstalledTo = $pluginDestination
    mcpInstalledTo = $resolvedMcpDestination
    mcpExecutable = $mcpExecutable
    dspSource = [string]$location.source
    integrityVerified = $true
} | ConvertTo-Json -Depth 3
