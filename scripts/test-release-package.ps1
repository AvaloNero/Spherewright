[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,
    [ValidateRange(1, 1000)]
    [int]$ExpectedMinimumToolCount = 1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$resolvedPackage = [IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $resolvedPackage -PathType Leaf)) {
    throw "Release package not found: $resolvedPackage"
}

$checksumPath = "$resolvedPackage.sha256"
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
    throw "Release checksum sidecar not found: $checksumPath"
}
$checksumLine = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
$expectedZipHash = ($checksumLine -split '\s+')[0]
$actualZipHash = (Get-FileHash -LiteralPath $resolvedPackage -Algorithm SHA256).Hash
if (-not [string]::Equals($actualZipHash, $expectedZipHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Release zip SHA-256 does not match its sidecar.'
}

$smokeParent = [IO.Path]::GetFullPath((Join-Path $repoRoot '.local\release-smoke'))
New-Item -ItemType Directory -Path $smokeParent -Force | Out-Null
$smokeRoot = Join-Path $smokeParent ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $smokeRoot | Out-Null
$process = $null

try {
    Expand-Archive -LiteralPath $resolvedPackage -DestinationPath $smokeRoot
    $topLevel = @(Get-ChildItem -LiteralPath $smokeRoot -Directory)
    if ($topLevel.Count -ne 1) {
        throw 'Release zip must contain exactly one top-level package directory.'
    }
    $packageRoot = $topLevel[0].FullName
    $manifestPath = Join-Path $packageRoot 'manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if (-not [string]::Equals([string]$manifest.productVersion, [string]$manifest.version, [StringComparison]::Ordinal)) {
        throw 'The release manifest product version does not match its package version.'
    }
    foreach ($entry in @($manifest.files)) {
        $relativePath = [string]$entry.path
        if ([string]::IsNullOrWhiteSpace($relativePath) -or [IO.Path]::IsPathRooted($relativePath)) {
            throw 'The release manifest contains an unsafe file path.'
        }
        $candidate = [IO.Path]::GetFullPath((Join-Path $packageRoot ($relativePath -replace '/', [IO.Path]::DirectorySeparatorChar)))
        $packagePrefix = $packageRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        if (-not $candidate.StartsWith($packagePrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The release manifest contains a path outside the package.'
        }
        $actualFileHash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash
        if (-not [string]::Equals($actualFileHash, [string]$entry.sha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Release file SHA-256 mismatch: $relativePath"
        }
    }

    $executable = Join-Path $packageRoot 'mcp\Spherewright.Mcp.exe'
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $executable
    $startInfo.WorkingDirectory = Split-Path -Parent $executable
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw 'The packaged MCP executable did not start.'
    }

    $initialize = [ordered]@{
        jsonrpc = '2.0'
        id = 1
        method = 'initialize'
        params = [ordered]@{
            protocolVersion = '2025-06-18'
            capabilities = @{}
            clientInfo = [ordered]@{
                name = 'spherewright-release-smoke'
                version = '0.1.0'
            }
        }
    } | ConvertTo-Json -Depth 8 -Compress
    $process.StandardInput.WriteLine($initialize)
    $process.StandardInput.Flush()
    $initializeTask = $process.StandardOutput.ReadLineAsync()
    if (-not $initializeTask.Wait(10000)) {
        throw 'The packaged MCP initialize request timed out.'
    }
    $initializeResponse = $initializeTask.Result | ConvertFrom-Json
    if ([int]$initializeResponse.id -ne 1 -or -not $initializeResponse.result.serverInfo) {
        throw 'The packaged MCP returned an invalid initialize response.'
    }
    $serverVersion = [Version]([string]$initializeResponse.result.serverInfo.version)
    $serverProductVersion = "$($serverVersion.Major).$($serverVersion.Minor).$($serverVersion.Build)"
    $manifestVersionCore = ([string]$manifest.productVersion -split '-', 2)[0]
    if (-not [string]::Equals($serverProductVersion, $manifestVersionCore, [StringComparison]::Ordinal)) {
        throw "The packaged MCP version $serverProductVersion does not match product version core $manifestVersionCore."
    }

    $initialized = @{ jsonrpc = '2.0'; method = 'notifications/initialized' } | ConvertTo-Json -Compress
    $listTools = @{ jsonrpc = '2.0'; id = 2; method = 'tools/list'; params = @{} } | ConvertTo-Json -Depth 5 -Compress
    $process.StandardInput.WriteLine($initialized)
    $process.StandardInput.WriteLine($listTools)
    $process.StandardInput.Flush()
    $listTask = $process.StandardOutput.ReadLineAsync()
    if (-not $listTask.Wait(10000)) {
        throw 'The packaged MCP tools/list request timed out.'
    }
    $listResponse = $listTask.Result | ConvertFrom-Json
    $tools = @($listResponse.result.tools)
    if ($tools.Count -lt $ExpectedMinimumToolCount) {
        throw "The packaged MCP exposed only $($tools.Count) tools; expected at least $ExpectedMinimumToolCount."
    }

    [pscustomobject]@{
        version = [string]$manifest.version
        productVersion = [string]$manifest.productVersion
        zipSha256 = $actualZipHash.ToLowerInvariant()
        manifestFileCount = @($manifest.files).Count
        protocolVersion = [string]$initializeResponse.result.protocolVersion
        serverName = [string]$initializeResponse.result.serverInfo.name
        serverVersion = [string]$initializeResponse.result.serverInfo.version
        toolCount = $tools.Count
        hasSessionState = $tools.name -contains 'spherewright_get_session_state'
        hasStationConfiguration = $tools.name -contains 'spherewright_prepare_configure_building'
        verified = $true
    } | ConvertTo-Json -Depth 3
} finally {
    if ($process -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(5000) | Out-Null
    }
    if ($process) {
        $process.Dispose()
    }

    $smokePrefix = $smokeParent.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedSmokeRoot = [IO.Path]::GetFullPath($smokeRoot)
    if ($resolvedSmokeRoot.StartsWith($smokePrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedSmokeRoot)) {
        Remove-Item -LiteralPath $resolvedSmokeRoot -Recurse -Force
    }
}
