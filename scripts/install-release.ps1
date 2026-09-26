[CmdletBinding()]
param(
    [string]$DspDir,
    [string]$McpDestination,
    [switch]$Force,
    [switch]$PreflightOnly,
    [switch]$StageOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($PreflightOnly -and $StageOnly) {
    throw '-PreflightOnly and -StageOnly are mutually exclusive.'
}

function Test-InstallPathOverlap {
    param(
        [Parameter(Mandatory)][string]$First,
        [Parameter(Mandatory)][string]$Second
    )

    $firstFull = [IO.Path]::GetFullPath($First).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $secondFull = [IO.Path]::GetFullPath($Second).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if ([string]::Equals($firstFull, $secondFull, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $firstPrefix = $firstFull + [IO.Path]::DirectorySeparatorChar
    $secondPrefix = $secondFull + [IO.Path]::DirectorySeparatorChar
    return $firstPrefix.StartsWith($secondPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        $secondPrefix.StartsWith($firstPrefix, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-InstallDestinationAncestors {
    param([Parameter(Mandatory)][string]$Destination)

    $ancestor = [IO.Path]::GetFullPath($Destination)
    while ($ancestor) {
        $item = Get-Item -LiteralPath $ancestor -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Reparse-point installation destinations are forbidden.'
        }

        $parent = [IO.Path]::GetDirectoryName($ancestor)
        if ([string]::IsNullOrEmpty($parent) -or [string]::Equals($parent, $ancestor, [StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $ancestor = $parent
    }
}

function Test-ApprovedDestinationDirectory {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles
    )

    $prefix = $RelativePath + '/'
    foreach ($expected in $ExpectedFiles.Keys) {
        if ($expected.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Assert-NoReparsePointBelow {
    param([Parameter(Mandatory)][string]$Root)

    $directories = [Collections.Generic.Stack[string]]::new()
    $directories.Push($Root)
    while ($directories.Count -gt 0) {
        $directory = $directories.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $directory -Force)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse-point installation destinations are forbidden.' }
            if ($item.PSIsContainer) { $directories.Push($item.FullName) }
        }
    }
}

function Assert-ApprovedDestinationTree {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles,
        [string[]]$PreservedDirectories = @(),
        [switch]$RequireComplete
    )

    $rootItem = Get-Item -LiteralPath $Root -Force -ErrorAction SilentlyContinue
    if ($null -eq $rootItem) {
        if ($RequireComplete) { throw "Installed destination is missing: $Root" }
        return
    }
    if (-not $rootItem.PSIsContainer) { throw "Installation destination must be a directory: $Root" }
    if ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse-point installation destinations are forbidden.' }

    $directories = [Collections.Generic.Stack[object]]::new()
    $directories.Push([pscustomobject]@{ path = $rootItem.FullName; relative = '' })
    while ($directories.Count -gt 0) {
        $current = $directories.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $current.path -Force)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse-point installation destinations are forbidden.' }
            $relative = if ([string]::IsNullOrEmpty([string]$current.relative)) { $item.Name } else { "$($current.relative)/$($item.Name)" }
            if ($item.PSIsContainer) {
                if ($PreservedDirectories -contains $relative) {
                    Assert-NoReparsePointBelow -Root $item.FullName
                    continue
                }
                if (-not (Test-ApprovedDestinationDirectory -RelativePath $relative -ExpectedFiles $ExpectedFiles)) {
                    throw "Unapproved installation directory: $relative"
                }
                $directories.Push([pscustomobject]@{ path = $item.FullName; relative = $relative })
                continue
            }

            if (-not $ExpectedFiles.ContainsKey($relative)) { throw "Unapproved installation file: $relative" }
            if ($RequireComplete) {
                $actual = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
                if (-not [string]::Equals($actual, $ExpectedFiles[$relative], [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Installed file integrity verification failed: $relative"
                }
            }
        }
    }

    if ($RequireComplete) {
        foreach ($relative in $ExpectedFiles.Keys) {
            $candidate = Join-Path $Root ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Installed file is missing: $relative" }
        }
    }
}

function Assert-NoStageResidue {
    param([Parameter(Mandatory)][string]$Parent)

    $item = Get-Item -LiteralPath $Parent -Force -ErrorAction SilentlyContinue
    if ($null -eq $item) { return }
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Staging parent must be a non-reparse directory.'
    }
    foreach ($child in @(Get-ChildItem -LiteralPath $item.FullName -Force)) {
        if ($child.Name -like '.spherewright-stage-*') {
            throw 'A prior Spherewright staging residue must be inspected before another staging attempt.'
        }
    }
}

function New-StagePayloadPlan {
    param(
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][string]$StageParent,
        [Parameter(Mandatory)][string]$Role,
        [Parameter(Mandatory)][string]$OperationId,
        [string]$ForbiddenScanRoot,
        [string[]]$ProtectedPaths = @()
    )

    $targetFull = [IO.Path]::GetFullPath($Target)
    $parentFull = [IO.Path]::GetFullPath($StageParent)
    $stageRoot = Join-Path $parentFull ('.spherewright-stage-' + $OperationId + '-' + $Role)
    $payload = Join-Path $stageRoot 'payload'
    $parentItem = Get-Item -LiteralPath $parentFull -Force -ErrorAction SilentlyContinue
    if ($null -eq $parentItem -or -not $parentItem.PSIsContainer) {
        throw 'Staging parent must already exist as a directory.'
    }
    if (-not [string]::Equals([IO.Path]::GetPathRoot($targetFull), [IO.Path]::GetPathRoot($stageRoot), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Staging must be created on the same volume as its live target.'
    }
    if ($stageRoot.TrimEnd('\', '/') -eq [IO.Path]::GetPathRoot($stageRoot).TrimEnd('\', '/')) {
        throw 'A staging directory cannot be a filesystem root.'
    }
    if (Test-Path -LiteralPath $stageRoot) { throw 'A unique staging directory already exists and must not be overwritten.' }
    Assert-InstallDestinationAncestors -Destination $stageRoot
    Assert-NoStageResidue -Parent $parentFull
    foreach ($protected in $ProtectedPaths) {
        if (Test-InstallPathOverlap -First $stageRoot -Second $protected) { throw 'Staging, package, and live target trees must not overlap.' }
    }
    if (-not [string]::IsNullOrWhiteSpace($ForbiddenScanRoot) -and (Test-InstallPathOverlap -First $stageRoot -Second $ForbiddenScanRoot)) {
        throw 'Plugin staging must remain outside the BepInEx/plugins recursive scan range.'
    }
    return [pscustomobject]@{ role=$Role; root=$stageRoot; payload=$payload; target=$targetFull }
}

function Copy-VerifiedPayloadToStage {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][object]$Stage,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles
    )

    [void][IO.Directory]::CreateDirectory($Stage.payload)
    $stageRootItem = Get-Item -LiteralPath $Stage.root -Force
    if (($stageRootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $stageRootItem.PSIsContainer) {
        throw 'Staging root must remain a non-reparse directory.'
    }
    foreach ($relative in @($ExpectedFiles.Keys | Sort-Object)) {
        $source = Join-Path $SourceRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        $destination = Join-Path $Stage.payload ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        $destinationParent = [IO.Path]::GetDirectoryName($destination)
        [void][IO.Directory]::CreateDirectory($destinationParent)
        $parentItem = Get-Item -LiteralPath $destinationParent -Force
        if (($parentItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $parentItem.PSIsContainer) {
            throw 'Staging payload parent must remain a non-reparse directory.'
        }
        Copy-Item -LiteralPath $source -Destination $destination -Force -ErrorAction Stop
    }
    Assert-NoReparsePointBelow -Root $Stage.root
    $children = @(Get-ChildItem -LiteralPath $Stage.root -Force)
    if ($children.Count -ne 1 -or -not $children[0].PSIsContainer -or $children[0].Name -cne 'payload') {
        throw 'Staging root contains an unapproved residual entry.'
    }
    Assert-ApprovedDestinationTree -Root $Stage.payload -ExpectedFiles $ExpectedFiles -RequireComplete
}

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
$declaredFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$declaredHashes = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in @($manifest.files)) {
    $relativePath = [string]$entry.path
    if ([string]::IsNullOrWhiteSpace($relativePath) -or [IO.Path]::IsPathRooted($relativePath) -or
        $relativePath.Contains('\') -or $relativePath.Contains(':') -or
        @($relativePath.Split('/') | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0 -or
        $relativePath -ieq 'manifest.json' -or -not $declaredFiles.Add($relativePath)) {
        throw 'The release manifest contains an unsafe file path.'
    }
    $declaredHashes.Add($relativePath, [string]$entry.sha256)

    $candidate = [IO.Path]::GetFullPath((Join-Path $packageRoot ($relativePath -replace '/', [IO.Path]::DirectorySeparatorChar)))
    if (-not $candidate.StartsWith($packagePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The release manifest contains a path outside the package.'
    }

    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "A release file is missing: $relativePath"
    }

    $sourceAncestor = $candidate
    while ($sourceAncestor -and $sourceAncestor.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) {
        if ((Get-Item -LiteralPath $sourceAncestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse points are forbidden in release packages.' }
        $sourceAncestor = [IO.Path]::GetDirectoryName($sourceAncestor)
    }
    if ((Get-Item -LiteralPath $candidate -Force).Length -ne [long]$entry.size) {
        throw "Release size verification failed: $relativePath"
    }
    $actualHash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash
    if (-not [string]::Equals($actualHash, [string]$entry.sha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Release integrity verification failed: $relativePath"
    }
}

# Enumerate without following links. A verified subset must never authorize copying
# additional source files (including hidden files) into the user's installation.
$directories = [Collections.Generic.Stack[string]]::new()
$directories.Push($packageRoot)
while ($directories.Count -gt 0) {
    $directory = $directories.Pop()
    if ((Get-Item -LiteralPath $directory -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'Reparse points are forbidden in release packages.'
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $directory -Force)) {
        if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse points are forbidden in release packages.' }
        if ($file.PSIsContainer) { $directories.Push($file.FullName); continue }
        $relative = $file.FullName.Substring($packageRoot.Length + 1).Replace('\', '/')
        if ($relative -ine 'manifest.json' -and -not $declaredFiles.Contains($relative)) {
            throw "Unlisted release file: $relative"
        }
    }
}
$requiredPluginNames = @('Spherewright.Plugin.dll', 'Spherewright.Contracts.dll', 'Spherewright.Bridge.Core.dll', 'Newtonsoft.Json.dll')
foreach ($required in @('install.ps1', 'locate-dsp.ps1', 'mcp/Spherewright.Mcp.exe') + @($requiredPluginNames | ForEach-Object { "BepInEx/plugins/Spherewright/$_" })) {
    if (-not $declaredFiles.Contains($required)) { throw "Required release file is missing from the manifest: $required" }
}
$pluginEntries = @($manifest.files | Where-Object { ([string]$_.path).StartsWith('BepInEx/plugins/Spherewright/', [StringComparison]::OrdinalIgnoreCase) })
if ($pluginEntries.Count -ne $requiredPluginNames.Count) { throw 'The packaged Plugin must contain exactly four approved assemblies.' }
$pluginExpectedFiles = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($name in $requiredPluginNames) {
    $manifestPath = "BepInEx/plugins/Spherewright/$name"
    $pluginExpectedFiles.Add($name, $declaredHashes[$manifestPath])
}
$mcpExpectedFiles = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in @($manifest.files)) {
    $manifestPath = [string]$entry.path
    if ($manifestPath.StartsWith('mcp/', [StringComparison]::OrdinalIgnoreCase)) {
        $mcpExpectedFiles.Add($manifestPath.Substring(4), $declaredHashes[$manifestPath])
    }
}
if ($mcpExpectedFiles.Count -eq 0) { throw 'The release manifest contains no MCP files.' }

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
foreach ($destination in @($pluginDestination, $resolvedMcpDestination)) {
    if ($destination.TrimEnd('\', '/') -eq [IO.Path]::GetPathRoot($destination).TrimEnd('\', '/')) { throw 'An installation destination cannot be a filesystem root.' }
    Assert-InstallDestinationAncestors -Destination $destination
}
$installPaths = @($packageRoot, $pluginDestination, $resolvedMcpDestination)
for ($first = 0; $first -lt $installPaths.Count; $first++) {
    for ($second = $first + 1; $second -lt $installPaths.Count; $second++) {
        if (Test-InstallPathOverlap -First $installPaths[$first] -Second $installPaths[$second]) {
            throw 'Package, Plugin and MCP destinations must not overlap.'
        }
    }
}

# The protected runtime descriptor is outside both payload targets. The Plugin's
# `runtime-handoff` directory is the one preserved target child: it is checked
# for reparse points but never copied, deleted, or treated as package payload.
# Exact target trees are validated in place before any copy, so -Force cannot
# overlay any other unapproved content.
Assert-ApprovedDestinationTree -Root $pluginDestination -ExpectedFiles $pluginExpectedFiles -PreservedDirectories @('runtime-handoff')
Assert-ApprovedDestinationTree -Root $resolvedMcpDestination -ExpectedFiles $mcpExpectedFiles
if ((Test-Path -LiteralPath $resolvedMcpDestination -PathType Container) -and
    @(Get-ChildItem -LiteralPath $resolvedMcpDestination -Force).Count -gt 0 -and
    -not $Force) {
    throw "The MCP destination already contains files. Re-run with -Force to reinstall this version: $resolvedMcpDestination"
}

$mcpSource = Join-Path $packageRoot 'mcp'
if (-not (Test-Path -LiteralPath (Join-Path $mcpSource 'Spherewright.Mcp.exe') -PathType Leaf)) {
    throw 'The packaged self-contained MCP executable is missing.'
}
if ($PreflightOnly) {
    [pscustomobject]@{ version=$version; integrityVerified=$true; exactFileSet=$true; preflightOnly=$true; installed=$false; transactionalUpgrade=$false } | ConvertTo-Json
    return
}

if ($StageOnly) {
    # Stage payloads only.  These roots are deliberately outside the Plugin scan
    # tree and outside both live targets; no live file, runtime descriptor, or
    # runtime-handoff entry is copied, moved, removed, or renamed here.
    $operationId = [guid]::NewGuid().ToString('N')
    $pluginScanRoot = Join-Path $gameRoot 'BepInEx\plugins'
    $pluginStageParent = Join-Path $gameRoot 'BepInEx'
    $mcpStageParent = [IO.Path]::GetDirectoryName($resolvedMcpDestination)
    if ([string]::IsNullOrWhiteSpace($mcpStageParent)) { throw 'MCP staging requires a concrete existing parent directory.' }
    $protectedPaths = @($packageRoot, $pluginDestination, $resolvedMcpDestination)
    $pluginStage = New-StagePayloadPlan -Target $pluginDestination -StageParent $pluginStageParent -Role 'plugin' -OperationId $operationId -ForbiddenScanRoot $pluginScanRoot -ProtectedPaths $protectedPaths
    $mcpStage = New-StagePayloadPlan -Target $resolvedMcpDestination -StageParent $mcpStageParent -Role 'mcp' -OperationId $operationId -ForbiddenScanRoot $pluginScanRoot -ProtectedPaths ($protectedPaths + @($pluginStage.root))
    if (Test-InstallPathOverlap -First $pluginStage.root -Second $mcpStage.root) { throw 'Plugin and MCP staging roots must be separate.' }
    Copy-VerifiedPayloadToStage -SourceRoot $pluginSource -Stage $pluginStage -ExpectedFiles $pluginExpectedFiles
    Copy-VerifiedPayloadToStage -SourceRoot $mcpSource -Stage $mcpStage -ExpectedFiles $mcpExpectedFiles
    [pscustomobject]@{
        version = $version
        operationId = $operationId
        pluginStagedTo = $pluginStage.payload
        mcpStagedTo = $mcpStage.payload
        integrityVerified = $true
        exactFileSet = $true
        installed = $false
        staged = $true
        liveTargetsUntouched = $true
        transactionalUpgrade = $false
    } | ConvertTo-Json -Depth 3
    return
}

New-Item -ItemType Directory -Path $pluginDestination -Force | Out-Null
foreach ($name in $requiredPluginNames) {
    Copy-Item -LiteralPath (Join-Path $pluginSource $name) -Destination (Join-Path $pluginDestination $name) -Force
}

New-Item -ItemType Directory -Path $resolvedMcpDestination -Force | Out-Null
foreach ($relative in @($mcpExpectedFiles.Keys | Sort-Object)) {
    $source = Join-Path $mcpSource ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
    $destination = Join-Path $resolvedMcpDestination ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

Assert-ApprovedDestinationTree -Root $pluginDestination -ExpectedFiles $pluginExpectedFiles -PreservedDirectories @('runtime-handoff') -RequireComplete
Assert-ApprovedDestinationTree -Root $resolvedMcpDestination -ExpectedFiles $mcpExpectedFiles -RequireComplete

$mcpExecutable = Join-Path $resolvedMcpDestination 'Spherewright.Mcp.exe'
[pscustomobject]@{
    version = $version
    pluginInstalledTo = $pluginDestination
    mcpInstalledTo = $resolvedMcpDestination
    mcpExecutable = $mcpExecutable
    dspSource = [string]$location.source
    integrityVerified = $true
    exactTargetFileSet = $true
    transactionalUpgrade = $false
} | ConvertTo-Json -Depth 3
