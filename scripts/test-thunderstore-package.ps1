[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$ExpectedVersion,
    [ValidatePattern('^[A-Za-z0-9_]+$')]
    [string]$TeamName = 'Arcueid_77',
    [string]$PackageName = 'Spherewright',
    [string]$ExpectedSourceCommit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$resolvedPackagePath = [IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Leaf)) {
    throw "Thunderstore package was not found: $resolvedPackagePath"
}

$checksumPath = "$resolvedPackagePath.sha256"
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
    throw "Thunderstore package checksum was not found: $checksumPath"
}
$checksumLine = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
$expectedZipHash = ($checksumLine -split '\s+')[0]
$actualZipHash = (Get-FileHash -LiteralPath $resolvedPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
if (-not [string]::Equals($actualZipHash, $expectedZipHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Thunderstore package checksum verification failed.'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
} finally {
    $archive.Dispose()
}

$requiredRootFiles = @('icon.png', 'README.md', 'manifest.json')
foreach ($requiredFile in $requiredRootFiles) {
    if ($entryNames -cnotcontains $requiredFile) {
        throw "Thunderstore root file is missing or has incorrect casing: $requiredFile"
    }
}
if ($entryNames | Where-Object { $_ -match '^[^/]+/$' -and $_ -notin @('plugins/') }) {
    throw 'Thunderstore package contains an unexpected top-level wrapper directory.'
}
if ($entryNames | Where-Object { $_ -like '*.pdb' }) {
    throw 'Thunderstore package must not contain debug symbols.'
}

$smokeParent = Join-Path $repoRoot '.local\thunderstore-static-check'
New-Item -ItemType Directory -Path $smokeParent -Force | Out-Null
$smokeRoot = Join-Path $smokeParent ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $smokeRoot -Force | Out-Null

try {
    Expand-Archive -LiteralPath $resolvedPackagePath -DestinationPath $smokeRoot

    $manifestPath = Join-Path $smokeRoot 'manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if (-not [string]::Equals([string]$manifest.name, $PackageName, [StringComparison]::Ordinal)) {
        throw "Unexpected Thunderstore package name: $($manifest.name)"
    }
    if (-not [string]::Equals([string]$manifest.version_number, $ExpectedVersion, [StringComparison]::Ordinal)) {
        throw "Unexpected Thunderstore package version: $($manifest.version_number)"
    }
    if ([string]::IsNullOrWhiteSpace([string]$manifest.website_url)) {
        throw 'Thunderstore website_url must not be empty.'
    }
    $description = [string]$manifest.description
    if ([string]::IsNullOrWhiteSpace($description) -or $description.Length -gt 250) {
        throw 'Thunderstore description must contain 1 to 250 characters.'
    }
    if (@($manifest.dependencies) -cnotcontains 'xiaoye97-BepInEx-5.4.17') {
        throw 'Thunderstore manifest is missing the supported BepInEx dependency.'
    }

    $iconPath = Join-Path $smokeRoot 'icon.png'
    $iconBytes = [IO.File]::ReadAllBytes($iconPath)
    $pngSignature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
    if ($iconBytes.Length -lt 24) {
        throw 'Thunderstore icon.png is truncated.'
    }
    for ($index = 0; $index -lt $pngSignature.Length; $index++) {
        if ($iconBytes[$index] -ne $pngSignature[$index]) {
            throw 'Thunderstore icon.png is not a PNG file.'
        }
    }
    $width = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($iconBytes, 16))
    $height = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($iconBytes, 20))
    if ($width -ne 256 -or $height -ne 256) {
        throw "Thunderstore icon.png must be exactly 256x256; found ${width}x${height}."
    }

    $sourceManifestPath = Join-Path $smokeRoot 'spherewright-manifest.json'
    if (-not (Test-Path -LiteralPath $sourceManifestPath -PathType Leaf)) {
        throw 'Spherewright integrity manifest is missing.'
    }
    $sourceManifest = Get-Content -LiteralPath $sourceManifestPath -Raw | ConvertFrom-Json
    if (-not [string]::Equals([string]$sourceManifest.version, $ExpectedVersion, [StringComparison]::Ordinal)) {
        throw "Unexpected Spherewright integrity-manifest version: $($sourceManifest.version)"
    }
    if (-not [string]::Equals([string]$sourceManifest.team, $TeamName, [StringComparison]::Ordinal)) {
        throw "Unexpected Thunderstore Team: $($sourceManifest.team)"
    }
    if (-not [string]::Equals([string]$sourceManifest.fullName, "$TeamName-$PackageName", [StringComparison]::Ordinal)) {
        throw "Unexpected Thunderstore full package name: $($sourceManifest.fullName)"
    }
    if ($ExpectedSourceCommit -and
        -not [string]::Equals([string]$sourceManifest.sourceCommit, $ExpectedSourceCommit, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected source commit: $($sourceManifest.sourceCommit)"
    }

    $requiredPayloadFiles = @(
        'plugins/Spherewright.Plugin.dll',
        'plugins/Spherewright.Contracts.dll',
        'plugins/Spherewright.Bridge.Core.dll',
        'plugins/Newtonsoft.Json.dll',
        'plugins/Spherewright.Mcp.exe'
    )
    foreach ($requiredFile in $requiredPayloadFiles) {
        $nativePath = Join-Path $smokeRoot ($requiredFile -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $nativePath -PathType Leaf)) {
            throw "Thunderstore payload file is missing: $requiredFile"
        }
    }

    $blockedNames = @('Assembly-CSharp.dll', 'UnityEngine.dll', '0Harmony.dll', 'BepInEx.dll', 'BepInEx.Core.dll')
    $blockedPayload = @(
        Get-ChildItem -LiteralPath $smokeRoot -File -Recurse |
            Where-Object { $blockedNames -ccontains $_.Name }
    )
    if ($blockedPayload.Count -gt 0) {
        throw "Thunderstore payload bundles a game or loader assembly: $($blockedPayload[0].Name)"
    }

    $listedFiles = @($sourceManifest.files)
    $actualFiles = @(
        Get-ChildItem -LiteralPath $smokeRoot -File -Recurse |
            Where-Object { $_.FullName -ne $sourceManifestPath } |
            ForEach-Object { $_.FullName.Substring($smokeRoot.Length + 1).Replace([IO.Path]::DirectorySeparatorChar, '/') } |
            Sort-Object
    )
    $listedPaths = @($listedFiles | ForEach-Object { [string]$_.path } | Sort-Object)
    if (($actualFiles -join "`n") -cne ($listedPaths -join "`n")) {
        throw 'Spherewright integrity manifest does not exactly cover the package payload.'
    }
    foreach ($entry in $listedFiles) {
        $payloadPath = Join-Path $smokeRoot (([string]$entry.path) -replace '/', [IO.Path]::DirectorySeparatorChar)
        $payloadHash = (Get-FileHash -LiteralPath $payloadPath -Algorithm SHA256).Hash
        if (-not [string]::Equals($payloadHash, [string]$entry.sha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Thunderstore payload hash verification failed: $($entry.path)"
        }
    }

    [pscustomobject]@{
        packagePath = $resolvedPackagePath
        version = $ExpectedVersion
        fullName = "$TeamName-$PackageName"
        sourceCommit = [string]$sourceManifest.sourceCommit
        sha256 = $actualZipHash
        entryCount = $entryNames.Count
        icon = "${width}x${height}"
        staticStructureVerified = $true
        runtimeBlackBoxTested = $false
    } | ConvertTo-Json -Depth 3
} finally {
    $resolvedSmokeParent = [IO.Path]::GetFullPath($smokeParent).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedSmokeRoot = [IO.Path]::GetFullPath($smokeRoot)
    if ($resolvedSmokeRoot.StartsWith($resolvedSmokeParent, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedSmokeRoot)) {
        Remove-Item -LiteralPath $resolvedSmokeRoot -Recurse -Force
    }
}
