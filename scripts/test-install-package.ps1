[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$PackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# This test operates only on an extracted package and an owned GUID-named temp
# tree. It deliberately creates DSP-shaped sentinel files but never starts DSP.
. (Join-Path $PSScriptRoot 'SpherewrightInstallTestSupport.ps1')

$script:TestInstallPackageOutputLimitBytes = 1MB
$script:TestInstallPackageTimeoutMilliseconds = 120000
$script:TestInstallPackageStages = 0

function Get-TestInstallPackageFullPath {
    param([Parameter(Mandatory)][string]$Path)

    return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-TestInstallPackageDescendant {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Context
    )

    $fullPath = Get-TestInstallPackageFullPath -Path $Path
    $fullRoot = Get-TestInstallPackageFullPath -Path $Root
    $prefix = $fullRoot + [IO.Path]::DirectorySeparatorChar
    Assert-TestInstallTransaction -Condition ($fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) -Message "$Context escapes the owned test root."
    return $fullPath
}

function ConvertTo-TestInstallPackageQuotedArgument {
    param([Parameter(Mandatory)][string]$Value)

    if ($Value.Contains('"')) { throw 'Test CLI arguments cannot contain a double quote.' }
    return '"' + $Value + '"'
}

function Read-TestInstallPackageBoundedLog {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    $length = (Get-Item -LiteralPath $Path -Force).Length
    if ($length -gt $script:TestInstallPackageOutputLimitBytes) {
        throw "Owned installer child output exceeds the $script:TestInstallPackageOutputLimitBytes byte test bound."
    }
    return [IO.File]::ReadAllText($Path)
}

function Get-TestInstallPackageTargetSnapshot {
    param([Parameter(Mandatory)][string]$TestRoot)

    $rootItem = Get-Item -LiteralPath $TestRoot -Force
    $entries = [Collections.Generic.List[string]]::new()
    $directories = [Collections.Generic.Stack[object]]::new()
    $directories.Push([pscustomobject]@{ path=$rootItem.FullName; relative='' })
    while ($directories.Count -gt 0) {
        $current = $directories.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $current.path -Force)) {
            $relative = if ([string]::IsNullOrEmpty([string]$current.relative)) { $item.Name } else { "$($current.relative)/$($item.Name)" }
            # The extracted package is immutable test input and cli is the owned
            # child-log sink. Every other subtree is an installation target,
            # stage parent, archive parent, or temporary DSP-shaped tree.
            if ([string]::IsNullOrEmpty([string]$current.relative) -and $item.Name -in @('package', 'cli')) { continue }
            $attributes = [int]$item.Attributes
            if ($item.PSIsContainer) {
                $entries.Add("D|$relative|$attributes")
                $directories.Push([pscustomobject]@{ path=$item.FullName; relative=$relative })
            } else {
                $entries.Add("F|$relative|$attributes|$((Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash)")
            }
        }
    }
    return (@($entries | Sort-Object) -join "`n")
}

function Invoke-TestInstallPackageCli {
    param(
        [Parameter(Mandatory)][string]$ShellPath,
        [Parameter(Mandatory)][string]$InstallerPath,
        [Parameter(Mandatory)][string]$DspDir,
        [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$LogDirectory,
        [switch]$PreflightOnly,
        [switch]$Force
    )

    $id = [guid]::NewGuid().ToString('N')
    $stdoutPath = Join-Path $LogDirectory "installer-$id.stdout"
    $stderrPath = Join-Path $LogDirectory "installer-$id.stderr"
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', (ConvertTo-TestInstallPackageQuotedArgument -Value $InstallerPath),
        '-DspDir', (ConvertTo-TestInstallPackageQuotedArgument -Value $DspDir),
        '-McpDestination', (ConvertTo-TestInstallPackageQuotedArgument -Value $McpDestination)
    )
    if ($PreflightOnly) { $arguments += '-PreflightOnly' }
    if ($Force) { $arguments += '-Force' }

    $process = $null
    $exitCode = $null
    try {
        $process = Start-Process -FilePath $ShellPath -ArgumentList ($arguments -join ' ') -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        # Windows PowerShell can otherwise expose a null ExitCode for a
        # redirected Start-Process child after it exits. Bind its process handle
        # while it is alive; the test still owns and alone may terminate it.
        $null = $process.Handle
        $stopwatch = [Diagnostics.Stopwatch]::StartNew()
        while (-not $process.HasExited) {
            foreach ($logPath in @($stdoutPath, $stderrPath)) {
                $log = Get-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
                if ($null -ne $log -and $log.Length -gt $script:TestInstallPackageOutputLimitBytes) {
                    try { $process.Kill() } catch { }
                    $process.WaitForExit(5000) | Out-Null
                    throw "Owned installer CLI child exceeded the $script:TestInstallPackageOutputLimitBytes byte output bound."
                }
            }
            if ($stopwatch.ElapsedMilliseconds -ge $script:TestInstallPackageTimeoutMilliseconds) {
                try { $process.Kill() } catch { }
                $process.WaitForExit(5000) | Out-Null
                throw "Owned installer CLI child timed out after $script:TestInstallPackageTimeoutMilliseconds ms."
            }
            [Threading.Thread]::Sleep(100)
        }
        $process.WaitForExit()
        $rawExitCode = $process.ExitCode
        if ($null -eq $rawExitCode) {
            $stdout = Read-TestInstallPackageBoundedLog -Path $stdoutPath
            $stderr = Read-TestInstallPackageBoundedLog -Path $stderrPath
            throw "Owned installer CLI child exited without an ExitCode. stdout=$($stdout.Length) chars; stderr=$($stderr.Length) chars."
        }
        $exitCode = [int]$rawExitCode
    } finally {
        if ($process) { $process.Dispose() }
    }

    if ($null -eq $exitCode) { throw 'Owned installer CLI child did not expose an exit code.' }
    return [pscustomobject]@{
        exitCode = $exitCode
        stdout = Read-TestInstallPackageBoundedLog -Path $stdoutPath
        stderr = Read-TestInstallPackageBoundedLog -Path $stderrPath
    }
}

function ConvertFrom-TestInstallPackageSuccessfulCli {
    param(
        [Parameter(Mandatory)][object]$CliResult,
        [Parameter(Mandatory)][string]$Context
    )

    if ($CliResult.exitCode -ne 0) {
        throw "$Context failed with installer CLI exit $($CliResult.exitCode): $($CliResult.stderr.Trim())"
    }
    if ([string]::IsNullOrWhiteSpace($CliResult.stdout)) {
        throw "$Context did not return installer JSON."
    }
    try {
        return $CliResult.stdout.Trim() | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$Context returned non-JSON installer stdout."
    }
}

function Assert-TestInstallPackageManifest {
    param([Parameter(Mandatory)][string]$PackageRoot)

    $manifestPath = Join-Path $PackageRoot 'manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw 'Extracted package is missing manifest.json.' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    Assert-TestInstallTransaction -Condition ([int]$manifest.schemaVersion -eq 1) -Message 'Unexpected package manifest schema.'
    Assert-TestInstallTransaction -Condition ([string]$manifest.package -ceq 'Spherewright') -Message 'Unexpected package manifest name.'
    Assert-TestInstallTransaction -Condition (-not [string]::IsNullOrWhiteSpace([string]$manifest.version)) -Message 'Package manifest is missing its version.'

    $expected = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach ($entry in @($manifest.files)) {
        $relative = [string]$entry.path
        if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or $relative.Contains('\') -or
            $relative.Contains(':') -or @($relative.Split('/') | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0 -or
            $relative -ceq 'manifest.json' -or $expected.ContainsKey($relative)) {
            throw 'Package manifest contains an unsafe or duplicate file entry.'
        }
        $expected.Add($relative, [string]$entry.sha256)
        if ([string]$entry.sha256 -notmatch '^[0-9a-fA-F]{64}$' -or [long]$entry.size -lt 0) {
            throw "Package manifest has invalid file metadata: $relative"
        }
        $candidate = [IO.Path]::GetFullPath((Join-Path $PackageRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)))
        $prefix = (Get-TestInstallPackageFullPath -Path $PackageRoot) + [IO.Path]::DirectorySeparatorChar
        if (-not $candidate.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            throw "Package manifest file is missing or escapes its root: $relative"
        }
        Assert-TestInstallTransaction -Condition ((Get-Item -LiteralPath $candidate -Force).Length -eq [long]$entry.size) -Message "Package manifest size mismatch: $relative"
        Assert-TestInstallTransaction -Condition ((Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash -ieq [string]$entry.sha256) -Message "Package manifest hash mismatch: $relative"
    }

    $actual = Get-TestInstallTransactionExpectedFiles -Root $PackageRoot
    Assert-TestInstallTransaction -Condition ($actual.ContainsKey('manifest.json')) -Message 'Extracted package is missing its manifest from the filesystem tree.'
    Assert-TestInstallTransaction -Condition ($actual.Count -eq ($expected.Count + 1)) -Message 'Extracted package contains an unlisted file.'
    foreach ($relative in $expected.Keys) {
        Assert-TestInstallTransaction -Condition ($actual.ContainsKey($relative) -and $actual[$relative] -ieq $expected[$relative]) -Message "Extracted package differs from its manifest: $relative"
    }
    return $manifest
}

function Assert-TestInstallPackagePayload {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$Expected,
        [switch]$PreserveRuntimeHandoff
    )

    $actual = Get-TestInstallTransactionExpectedFiles -Root $Root
    foreach ($relative in $actual.Keys) {
        if ($PreserveRuntimeHandoff -and $relative.StartsWith('runtime-handoff/', [StringComparison]::OrdinalIgnoreCase)) { continue }
        Assert-TestInstallTransaction -Condition ($Expected.ContainsKey($relative)) -Message "Installed payload contains an unapproved file: $relative"
        Assert-TestInstallTransaction -Condition ($actual[$relative] -ieq $Expected[$relative]) -Message "Installed payload hash mismatch: $relative"
    }
    foreach ($relative in $Expected.Keys) {
        Assert-TestInstallTransaction -Condition ($actual.ContainsKey($relative) -and $actual[$relative] -ieq $Expected[$relative]) -Message "Installed payload is missing or changed: $relative"
    }
}

function Assert-TestInstallPackagePairedCommit {
    param(
        [Parameter(Mandatory)][object]$Result,
        [Parameter(Mandatory)][string]$TestRoot,
        [Parameter(Mandatory)][string]$PluginDestination,
        [Parameter(Mandatory)][string]$McpDestination
    )

    $pluginRecordPath = Assert-TestInstallPackageDescendant -Path ([string]$Result.recordPath) -Root $TestRoot -Context 'Plugin transaction record'
    Assert-TestInstallTransaction -Condition ((Split-Path -Leaf $pluginRecordPath) -ceq 'progress.json') -Message 'Installer returned an unexpected transaction record name.'
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $pluginRecordPath -PathType Leaf) -Message 'Installer did not retain its Plugin transaction record.'
    $pluginRecord = Get-Content -LiteralPath $pluginRecordPath -Raw | ConvertFrom-Json
    $pluginArchive = Assert-TestInstallPackageDescendant -Path ([string]$pluginRecord.pluginArchive) -Root $TestRoot -Context 'Plugin archive'
    $mcpArchive = Assert-TestInstallPackageDescendant -Path ([string]$pluginRecord.mcpArchive) -Root $TestRoot -Context 'MCP archive'
    Assert-TestInstallTransaction -Condition ($pluginRecordPath -ceq (Join-Path $pluginArchive 'transaction\progress.json')) -Message 'Installer returned a record path outside its Plugin archive.'
    $mcpRecordPath = Join-Path $mcpArchive 'transaction\progress.json'
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $mcpRecordPath -PathType Leaf) -Message 'Installer did not retain its mirrored MCP transaction record.'
    $mcpRecord = Get-Content -LiteralPath $mcpRecordPath -Raw | ConvertFrom-Json

    foreach ($record in @($pluginRecord, $mcpRecord)) {
        Assert-TestInstallTransaction -Condition ([int]$record.schemaVersion -eq 1) -Message 'Terminal transaction record has an unexpected schema.'
        Assert-TestInstallTransaction -Condition ([string]$record.operationId -ceq [string]$Result.operationId) -Message 'Terminal transaction record has an unexpected operation ID.'
        Assert-TestInstallTransaction -Condition ([string]$record.status -ceq 'committed') -Message 'Terminal transaction record is not committed.'
        Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageFullPath -Path ([string]$record.pluginArchive)) -ceq $pluginArchive) -Message 'Paired transaction records disagree about the Plugin archive.'
        Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageFullPath -Path ([string]$record.mcpArchive)) -ceq $mcpArchive) -Message 'Paired transaction records disagree about the MCP archive.'
        Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageFullPath -Path ([string]$record.pluginDestination)) -ceq (Get-TestInstallPackageFullPath -Path $PluginDestination)) -Message 'Paired transaction records disagree about the Plugin target.'
        Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageFullPath -Path ([string]$record.mcpDestination)) -ceq (Get-TestInstallPackageFullPath -Path $McpDestination)) -Message 'Paired transaction records disagree about the MCP target.'
    }
}

function Assert-TestInstallPackageNoMarker {
    param([Parameter(Mandatory)][string]$MarkerPath)
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $MarkerPath)) -Message 'Installer left its pending startup marker behind.'
}

$resolvedPackagePath = [IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Leaf)) { throw "Package ZIP was not found: $resolvedPackagePath" }
$checksumPath = "$resolvedPackagePath.sha256"
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { throw "Package checksum sidecar was not found: $checksumPath" }
$checksumParts = @((Get-Content -LiteralPath $checksumPath -Raw).Trim() -split '\s+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($checksumParts.Count -ne 2 -or $checksumParts[0] -notmatch '^[0-9a-fA-F]{64}$' -or $checksumParts[1] -cne [IO.Path]::GetFileName($resolvedPackagePath)) {
    throw 'Package checksum sidecar has an unexpected shape.'
}
$packageHash = (Get-FileHash -LiteralPath $resolvedPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
Assert-TestInstallTransaction -Condition ($packageHash -ceq $checksumParts[0].ToLowerInvariant()) -Message 'Package ZIP does not match its checksum sidecar.'

$testParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = Join-Path $testParent ('spherewright-install-package-' + [guid]::NewGuid().ToString('N'))
$shellPath = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
[void][IO.Directory]::CreateDirectory($testRoot)

try {
    $extractRoot = Join-Path $testRoot 'package'
    Expand-Archive -LiteralPath $resolvedPackagePath -DestinationPath $extractRoot
    $topDirectories = @(Get-ChildItem -LiteralPath $extractRoot -Directory -Force)
    $topFiles = @(Get-ChildItem -LiteralPath $extractRoot -File -Force)
    Assert-TestInstallTransaction -Condition ($topDirectories.Count -eq 1 -and $topFiles.Count -eq 0) -Message 'Package ZIP must contain exactly one top-level directory.'
    $packageRoot = $topDirectories[0].FullName
    $manifest = Assert-TestInstallPackageManifest -PackageRoot $packageRoot
    $installerPath = Join-Path $packageRoot 'install.ps1'
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $installerPath -PathType Leaf) -Message 'Extracted package has no installer entry point.'

    $gameRoot = Join-Path $testRoot 'g'
    $pluginDestination = Join-Path $gameRoot 'BepInEx\plugins\Spherewright'
    $mcpDestination = Join-Path $testRoot 'm\installed'
    $markerPath = Join-Path $gameRoot 'BepInEx\.spherewright-install-pending.json'
    $logs = Join-Path $testRoot 'cli'
    foreach ($sentinel in @(
        (Join-Path $gameRoot 'DSPGAME.exe'),
        (Join-Path $gameRoot 'DSPGAME_Data\Managed\Assembly-CSharp.dll'),
        (Join-Path $gameRoot 'BepInEx\core\BepInEx.dll')
    )) {
        Write-TestInstallTransactionFile -Path $sentinel -Content 'synthetic sentinel: never launched'
    }
    [void][IO.Directory]::CreateDirectory($logs)
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $pluginDestination)) -Message 'First-install Plugin target unexpectedly exists.'
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $mcpDestination)) -Message 'First-install MCP target unexpectedly exists.'

    # 1. Preflight validates the real ZIP/package without creating either target.
    $targetBeforePreflight = Get-TestInstallPackageTargetSnapshot -TestRoot $testRoot
    $preflight = ConvertFrom-TestInstallPackageSuccessfulCli -CliResult (Invoke-TestInstallPackageCli -ShellPath $shellPath -InstallerPath $installerPath -DspDir $gameRoot -McpDestination $mcpDestination -LogDirectory $logs -PreflightOnly) -Context 'Preflight'
    Assert-TestInstallTransaction -Condition ([bool]$preflight.preflightOnly -and -not [bool]$preflight.installed -and [bool]$preflight.integrityVerified -and [bool]$preflight.exactFileSet) -Message 'Preflight did not report the expected zero-write result.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageTargetSnapshot -TestRoot $testRoot) -ceq $targetBeforePreflight) -Message 'Preflight changed a synthetic target, stage, or archive parent.'
    Assert-TestInstallPackageNoMarker -MarkerPath $markerPath
    $script:TestInstallPackageStages++

    $pluginExpected = Get-TestInstallTransactionExpectedFiles -Root (Join-Path $packageRoot 'BepInEx\plugins\Spherewright')
    $mcpExpected = Get-TestInstallTransactionExpectedFiles -Root (Join-Path $packageRoot 'mcp')

    # 2. First install starts with both live payload roots absent.
    $firstInstall = ConvertFrom-TestInstallPackageSuccessfulCli -CliResult (Invoke-TestInstallPackageCli -ShellPath $shellPath -InstallerPath $installerPath -DspDir $gameRoot -McpDestination $mcpDestination -LogDirectory $logs) -Context 'First install'
    Assert-TestInstallTransaction -Condition ([bool]$firstInstall.installed -and [string]$firstInstall.status -ceq 'committed' -and [bool]$firstInstall.mcpHandshakeVerified) -Message 'First install did not reach committed metadata-verified state.'
    Assert-TestInstallPackagePayload -Root $pluginDestination -Expected $pluginExpected
    Assert-TestInstallPackagePayload -Root $mcpDestination -Expected $mcpExpected
    Assert-TestInstallPackagePairedCommit -Result $firstInstall -TestRoot $testRoot -PluginDestination $pluginDestination -McpDestination $mcpDestination
    Assert-TestInstallPackageNoMarker -MarkerPath $markerPath
    $script:TestInstallPackageStages++

    # 3. A populated same-version target without -Force must reject before any write.
    $targetBeforeReject = Get-TestInstallPackageTargetSnapshot -TestRoot $testRoot
    $rejected = Invoke-TestInstallPackageCli -ShellPath $shellPath -InstallerPath $installerPath -DspDir $gameRoot -McpDestination $mcpDestination -LogDirectory $logs
    $rejectedOutput = ($rejected.stdout + "`n" + $rejected.stderr).Trim()
    Assert-TestInstallTransaction -Condition ($rejected.exitCode -ne 0) -Message "Same-version reinstall unexpectedly succeeded without -Force: $rejectedOutput"
    Assert-TestInstallTransaction -Condition ($rejectedOutput -match 'MCP destination already contains files') -Message "Same-version reinstall rejected for an unexpected reason: $rejectedOutput"
    Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageTargetSnapshot -TestRoot $testRoot) -ceq $targetBeforeReject) -Message 'Rejected same-version reinstall changed a synthetic target, stage, or archive parent.'
    Assert-TestInstallPackageNoMarker -MarkerPath $markerPath
    $script:TestInstallPackageStages++

    # 4. A test-owned handoff sentinel survives a forced same-version reinstall.
    $handoffSentinel = Join-Path $pluginDestination 'runtime-handoff\test-sentinel.json'
    Write-TestInstallTransactionFile -Path $handoffSentinel -Content 'test-owned protected runtime handoff sentinel'
    $handoffBefore = Get-TestInstallTransactionTree -Root (Split-Path -Parent $handoffSentinel)
    $forcedInstall = ConvertFrom-TestInstallPackageSuccessfulCli -CliResult (Invoke-TestInstallPackageCli -ShellPath $shellPath -InstallerPath $installerPath -DspDir $gameRoot -McpDestination $mcpDestination -LogDirectory $logs -Force) -Context 'Forced same-version reinstall'
    Assert-TestInstallTransaction -Condition ([bool]$forcedInstall.installed -and [string]$forcedInstall.status -ceq 'committed' -and [bool]$forcedInstall.mcpHandshakeVerified) -Message 'Forced same-version reinstall did not reach committed metadata-verified state.'
    Assert-TestInstallPackagePayload -Root $pluginDestination -Expected $pluginExpected -PreserveRuntimeHandoff
    Assert-TestInstallPackagePayload -Root $mcpDestination -Expected $mcpExpected
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root (Split-Path -Parent $handoffSentinel)) -ceq $handoffBefore) -Message 'Forced reinstall changed the test-owned runtime-handoff sentinel.'
    Assert-TestInstallPackagePairedCommit -Result $forcedInstall -TestRoot $testRoot -PluginDestination $pluginDestination -McpDestination $mcpDestination
    Assert-TestInstallPackageNoMarker -MarkerPath $markerPath
    $script:TestInstallPackageStages++

    [pscustomobject][ordered]@{
        package = [IO.Path]::GetFileName($resolvedPackagePath)
        zipSha256 = $packageHash
        version = [string]$manifest.version
        stages = $script:TestInstallPackageStages
        preflightZeroTargetWrites = $true
        firstInstall = 'committed'
        sameVersionWithoutForce = 'rejected_unchanged'
        forceReinstall = 'committed'
        runtimeHandoffSentinelPreserved = $true
        realPackagedMcpProbe = $true
        gameLaunches = 0
    } | ConvertTo-Json -Compress
} finally {
    $resolvedTempParent = [IO.Path]::GetFullPath($testParent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $leaf = Split-Path -Leaf $resolvedTestRoot
    if (-not $resolvedTestRoot.StartsWith($resolvedTempParent, [StringComparison]::OrdinalIgnoreCase) -or
        $leaf -notmatch '^spherewright-install-package-[0-9a-f]{32}$') {
        throw "Refusing to clean an unexpected test path: $resolvedTestRoot"
    }
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
