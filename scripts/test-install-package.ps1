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
        [switch]$StageOnly,
        [switch]$Force
    )

    $arguments = @(
        '-DspDir', $DspDir,
        '-McpDestination', $McpDestination
    )
    if ($PreflightOnly) { $arguments += '-PreflightOnly' }
    if ($StageOnly) { $arguments += '-StageOnly' }
    if ($Force) { $arguments += '-Force' }
    return Invoke-TestInstallPackageAuxiliaryScript -ShellPath $ShellPath -ScriptPath $InstallerPath -ScriptArguments $arguments -LogDirectory $LogDirectory -Label 'installer-cli'
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

function Invoke-TestInstallPackageAuxiliaryScript {
    param(
        [Parameter(Mandatory)][string]$ShellPath,
        [Parameter(Mandatory)][string]$ScriptPath,
        [Parameter(Mandatory)][string[]]$ScriptArguments,
        [Parameter(Mandatory)][string]$LogDirectory,
        [Parameter(Mandatory)][string]$Label
    )

    $id = [guid]::NewGuid().ToString('N')
    $safeLabel = $Label -replace '[^A-Za-z0-9_-]', '-'
    $stdoutPath = Join-Path $LogDirectory "$safeLabel-$id.stdout"
    $stderrPath = Join-Path $LogDirectory "$safeLabel-$id.stderr"
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (ConvertTo-TestInstallPackageQuotedArgument -Value $ScriptPath))
    foreach ($argument in $ScriptArguments) {
        $arguments += (ConvertTo-TestInstallPackageQuotedArgument -Value $argument)
    }

    $process = $null
    $exitCode = $null
    try {
        $process = Start-Process -FilePath $ShellPath -ArgumentList ($arguments -join ' ') -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $null = $process.Handle
        $stopwatch = [Diagnostics.Stopwatch]::StartNew()
        while (-not $process.HasExited) {
            foreach ($logPath in @($stdoutPath, $stderrPath)) {
                $log = Get-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
                if ($null -ne $log -and $log.Length -gt $script:TestInstallPackageOutputLimitBytes) {
                    try { $process.Kill() } catch { }
                    $process.WaitForExit(5000) | Out-Null
                    throw "Owned $Label child exceeded the $script:TestInstallPackageOutputLimitBytes byte output bound."
                }
            }
            if ($stopwatch.ElapsedMilliseconds -ge $script:TestInstallPackageTimeoutMilliseconds) {
                try { $process.Kill() } catch { }
                $process.WaitForExit(5000) | Out-Null
                throw "Owned $Label child timed out after $script:TestInstallPackageTimeoutMilliseconds ms."
            }
            [Threading.Thread]::Sleep(100)
        }
        $process.WaitForExit()
        $rawExitCode = $process.ExitCode
        if ($null -eq $rawExitCode) {
            $stdout = Read-TestInstallPackageBoundedLog -Path $stdoutPath
            $stderr = Read-TestInstallPackageBoundedLog -Path $stderrPath
            throw "Owned $Label child exited without an ExitCode. stdout=$($stdout.Length) chars; stderr=$($stderr.Length) chars."
        }
        $exitCode = [int]$rawExitCode
    } finally {
        if ($null -ne $process) { $process.Dispose() }
    }

    return [pscustomobject]@{
        exitCode = $exitCode
        stdout = Read-TestInstallPackageBoundedLog -Path $stdoutPath
        stderr = Read-TestInstallPackageBoundedLog -Path $stderrPath
    }
}

function Invoke-TestInstallPackageCrashTransactionChild {
    param(
        [Parameter(Mandatory)][string]$ShellPath,
        [Parameter(Mandatory)][string]$PackageRoot,
        [Parameter(Mandatory)][string]$PluginStagePayload,
        [Parameter(Mandatory)][string]$McpStagePayload,
        [Parameter(Mandatory)][string]$PluginDestination,
        [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$LogDirectory
    )

    $id = [guid]::NewGuid().ToString('N')
    $childPath = Join-Path $LogDirectory ('transaction-interruption-' + $id + '.ps1')
    $interruptionSentinelPath = Join-Path $LogDirectory ('transaction-interruption-' + $id + '.step')
    $childSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackageRoot,
    [Parameter(Mandatory)][string]$PluginStagePayload,
    [Parameter(Mandatory)][string]$McpStagePayload,
    [Parameter(Mandatory)][string]$PluginDestination,
    [Parameter(Mandatory)][string]$McpDestination,
    [Parameter(Mandatory)][string]$InterruptionSentinelPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ExpectedStageFiles {
    param([Parameter(Mandatory)][string]$Root)

    $expected = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    $directories = [Collections.Generic.Stack[object]]::new()
    $directories.Push([pscustomobject]@{ path=$Root; relative='' })
    while ($directories.Count -gt 0) {
        $current = $directories.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $current.path -Force)) {
            $relative = if ([string]::IsNullOrEmpty([string]$current.relative)) { $item.Name } else { "$($current.relative)/$($item.Name)" }
            if ($item.PSIsContainer) {
                $directories.Push([pscustomobject]@{ path=$item.FullName; relative=$relative })
            } else {
                $expected.Add($relative, (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash)
            }
        }
    }
    return $expected
}

. (Join-Path $PackageRoot 'SpherewrightInstallTransaction.ps1')
. (Join-Path $PackageRoot 'Test-SpherewrightStagedMcp.ps1')
$manifest = Get-Content -LiteralPath (Join-Path $PackageRoot 'manifest.json') -Raw | ConvertFrom-Json
$version = [string]$manifest.version
$playbookPath = Join-Path $PackageRoot 'AGENT-PLAYBOOK.md'
$mcpStageRoot = Split-Path -Parent $McpStagePayload
$verifyInstalled = {
    param([string]$LiveMcpDirectory)
    Invoke-SpherewrightStagedMcpProbe -ExecutablePath (Join-Path $LiveMcpDirectory 'Spherewright.Mcp.exe') `
        -ExpectedVersion $version -ExpectedPlaybookPath $playbookPath `
        -IsolationDirectory (Join-Path $mcpStageRoot 'final-metadata-probe') | Out-Null
    return [pscustomobject]@{ packageProbe = 'real' }
}.GetNewClosure()
$crashAfterMainPromotion = {
    param([string]$Step)
    if ($Step -ceq 'progress-verify-installed') {
        $stream = [IO.File]::Open($InterruptionSentinelPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try {
            $bytes = [Text.Encoding]::UTF8.GetBytes('progress-verify-installed')
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush($true)
        } finally {
            $stream.Dispose()
        }
        [Console]::Error.WriteLine('Intentional package-test interruption after main Plugin promotion at progress-verify-installed.')
        [Console]::Error.Flush()
        [Environment]::FailFast('Intentional package-test interruption after main Plugin promotion.')
    }
}.GetNewClosure()

Invoke-SpherewrightInstallTransaction `
    -PluginStagePayload $PluginStagePayload -PluginDestination $PluginDestination -PluginExpectedFiles (Get-ExpectedStageFiles -Root $PluginStagePayload) `
    -McpStagePayload $McpStagePayload -McpDestination $McpDestination -McpExpectedFiles (Get-ExpectedStageFiles -Root $McpStagePayload) `
    -VerifyInstalled $verifyInstalled -BeforeMutation $crashAfterMainPromotion | Out-Null
throw 'The package interruption hook did not terminate its owned child.'
'@
    [IO.File]::WriteAllText($childPath, $childSource, [Text.UTF8Encoding]::new($false))
    $result = Invoke-TestInstallPackageAuxiliaryScript -ShellPath $ShellPath -ScriptPath $childPath -ScriptArguments @(
        '-PackageRoot', $PackageRoot,
        '-PluginStagePayload', $PluginStagePayload,
        '-McpStagePayload', $McpStagePayload,
        '-PluginDestination', $PluginDestination,
        '-McpDestination', $McpDestination,
        '-InterruptionSentinelPath', $interruptionSentinelPath
    ) -LogDirectory $LogDirectory -Label 'transaction-interruption'
    Assert-TestInstallTransaction -Condition ($result.exitCode -ne 0) -Message 'The owned package transaction child did not terminate at the intended interruption point.'
    Assert-TestInstallTransaction -Condition ((Test-Path -LiteralPath $interruptionSentinelPath -PathType Leaf) -and ((Get-Content -LiteralPath $interruptionSentinelPath -Raw -Encoding UTF8) -ceq 'progress-verify-installed')) -Message 'The owned package transaction child did not prove its exact interruption step.'
    Assert-TestInstallTransaction -Condition ($result.stderr -match 'Intentional package-test interruption after main Plugin promotion at progress-verify-installed') -Message 'The owned package transaction child did not report its intentional interruption on stderr.'
    return $result
}

function Invoke-TestInstallPackageRecoveryCli {
    param(
        [Parameter(Mandatory)][string]$ShellPath,
        [Parameter(Mandatory)][string]$RecoveryPath,
        [Parameter(Mandatory)][string]$PluginDestination,
        [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$OperationId,
        [Parameter(Mandatory)][string]$LogDirectory,
        [switch]$RestoreOriginal,
        [string]$ExpectedEvidenceHash
    )

    $arguments = @(
        '-PluginDestination', $PluginDestination,
        '-McpDestination', $McpDestination,
        '-OperationId', $OperationId
    )
    if ($RestoreOriginal) {
        $arguments += '-RestoreOriginal'
        $arguments += @('-ExpectedEvidenceHash', $ExpectedEvidenceHash)
    }
    return Invoke-TestInstallPackageAuxiliaryScript -ShellPath $ShellPath -ScriptPath $RecoveryPath -ScriptArguments $arguments -LogDirectory $LogDirectory -Label 'recovery-cli'
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

function Assert-TestInstallPackagePairedRollback {
    param(
        [Parameter(Mandatory)][string]$TestRoot,
        [Parameter(Mandatory)][string]$PluginStageParent,
        [Parameter(Mandatory)][string]$McpStageParent,
        [Parameter(Mandatory)][string]$OperationId,
        [Parameter(Mandatory)][string]$PluginDestination,
        [Parameter(Mandatory)][string]$McpDestination
    )

    $pluginArchive = Assert-TestInstallPackageDescendant -Path (Join-Path $PluginStageParent ('.spherewright-archive-' + $OperationId + '-plugin')) -Root $TestRoot -Context 'Plugin rollback archive'
    $mcpArchive = Assert-TestInstallPackageDescendant -Path (Join-Path $McpStageParent ('.spherewright-archive-' + $OperationId + '-mcp')) -Root $TestRoot -Context 'MCP rollback archive'
    foreach ($recordPath in @((Join-Path $pluginArchive 'transaction\progress.json'), (Join-Path $mcpArchive 'transaction\progress.json'))) {
        Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $recordPath -PathType Leaf) -Message 'Rollback did not retain a paired transaction record.'
        $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
        Assert-TestInstallTransaction -Condition ([int]$record.schemaVersion -eq 1 -and [string]$record.operationId -ceq $OperationId -and [string]$record.status -ceq 'rolled_back') -Message 'Rollback archive record is not terminal rolled_back for the staged operation.'
        Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageFullPath -Path ([string]$record.pluginArchive)) -ceq $pluginArchive -and (Get-TestInstallPackageFullPath -Path ([string]$record.mcpArchive)) -ceq $mcpArchive) -Message 'Paired rollback records disagree about their archive pair.'
        Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageFullPath -Path ([string]$record.pluginDestination)) -ceq (Get-TestInstallPackageFullPath -Path $PluginDestination) -and (Get-TestInstallPackageFullPath -Path ([string]$record.mcpDestination)) -ceq (Get-TestInstallPackageFullPath -Path $McpDestination)) -Message 'Paired rollback records disagree about their live targets.'
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
$testRoot = Join-Path $testParent ('swip-' + [guid]::NewGuid().ToString('N'))
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

    # 5. The archived package can explicitly restore a partial-root first install
    # after its own staged payload reaches main-Plugin promotion but never commits.
    # The interruption uses the transaction's existing internal test hook, not an
    # injection into install.ps1; it therefore does not claim an installer-CLI
    # crash test or a real DSP deployment.
    $recoveryRoot = Join-Path $testRoot 'r'
    $recoveryGameRoot = Join-Path $recoveryRoot 'g'
    $recoveryPluginDestination = Join-Path $recoveryGameRoot 'BepInEx\plugins\Spherewright'
    $recoveryMcpBase = Join-Path $recoveryRoot 'm'
    $recoveryMcpDestination = Join-Path $recoveryMcpBase 'installed'
    $recoveryMarkerPath = Join-Path $recoveryGameRoot 'BepInEx\.spherewright-install-pending.json'
    foreach ($sentinel in @(
        (Join-Path $recoveryGameRoot 'DSPGAME.exe'),
        (Join-Path $recoveryGameRoot 'DSPGAME_Data\Managed\Assembly-CSharp.dll'),
        (Join-Path $recoveryGameRoot 'BepInEx\core\BepInEx.dll')
    )) {
        Write-TestInstallTransactionFile -Path $sentinel -Content 'synthetic sentinel: never launched'
    }
    $recoveryHandoffSentinel = Join-Path $recoveryPluginDestination 'runtime-handoff\partial-root-sentinel.json'
    Write-TestInstallTransactionFile -Path $recoveryHandoffSentinel -Content 'partial-root handoff must remain unchanged'
    [void][IO.Directory]::CreateDirectory($recoveryMcpBase)
    $partialPluginBefore = Get-TestInstallTransactionTree -Root $recoveryPluginDestination
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Join-Path $recoveryPluginDestination 'Spherewright.Plugin.dll') -PathType Leaf) -and -not (Test-Path -LiteralPath $recoveryMcpDestination)) -Message 'Partial-root recovery baseline unexpectedly contains a package payload.'

    $staged = ConvertFrom-TestInstallPackageSuccessfulCli -CliResult (Invoke-TestInstallPackageCli -ShellPath $shellPath -InstallerPath $installerPath -DspDir $recoveryGameRoot -McpDestination $recoveryMcpDestination -LogDirectory $logs -StageOnly) -Context 'Partial-root staged package'
    Assert-TestInstallTransaction -Condition ([bool]$staged.staged -and -not [bool]$staged.installed -and [bool]$staged.integrityVerified -and [bool]$staged.exactFileSet -and [bool]$staged.mcpHandshakeVerified -and [string]$staged.operationId -match '^[0-9a-f]{32}$') -Message 'StageOnly did not return an exact staged package operation with its MCP handshake verified.'
    $stagedPluginPayload = Assert-TestInstallPackageDescendant -Path ([string]$staged.pluginStagedTo) -Root $testRoot -Context 'Staged Plugin payload'
    $stagedMcpPayload = Assert-TestInstallPackageDescendant -Path ([string]$staged.mcpStagedTo) -Root $testRoot -Context 'Staged MCP payload'
    Assert-TestInstallPackagePayload -Root $stagedPluginPayload -Expected $pluginExpected
    Assert-TestInstallPackagePayload -Root $stagedMcpPayload -Expected $mcpExpected
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root $recoveryPluginDestination) -ceq $partialPluginBefore -and -not (Test-Path -LiteralPath $recoveryMcpDestination)) -Message 'StageOnly modified the partial-root live baseline.'

    $crashResult = Invoke-TestInstallPackageCrashTransactionChild -ShellPath $shellPath -PackageRoot $packageRoot -PluginStagePayload $stagedPluginPayload -McpStagePayload $stagedMcpPayload -PluginDestination $recoveryPluginDestination -McpDestination $recoveryMcpDestination -LogDirectory $logs
    $recoveryPluginStageRoot = Split-Path -Parent $stagedPluginPayload
    $recoveryMcpStageRoot = Split-Path -Parent $stagedMcpPayload
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $recoveryMarkerPath -PathType Leaf) -Message 'The interrupted package transaction did not retain its pending marker.'
    $promotedMain = Join-Path $recoveryPluginDestination 'Spherewright.Plugin.dll'
    Assert-TestInstallTransaction -Condition ((Test-Path -LiteralPath $promotedMain -PathType Leaf) -and ((Get-FileHash -LiteralPath $promotedMain -Algorithm SHA256).Hash -ieq $pluginExpected['Spherewright.Plugin.dll'])) -Message 'The interruption did not occur after the real staged main Plugin payload was promoted.'
    foreach ($recordPath in @((Join-Path $recoveryPluginStageRoot 'transaction\progress.json'), (Join-Path $recoveryMcpStageRoot 'transaction\progress.json'))) {
        Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $recordPath -PathType Leaf) -Message 'The interrupted package transaction did not retain both pre-terminal records.'
        $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
        Assert-TestInstallTransaction -Condition ([string]$record.operationId -ceq [string]$staged.operationId -and [string]$record.status -notin @('committed', 'rolled_back') -and [string]$record.phase -ceq 'copy-plugin-main-last') -Message 'The intentional interruption did not stop immediately after main-Plugin promotion.'
    }

    $recoveryCli = Join-Path $packageRoot 'recover-install.ps1'
    $recoveryBeforePreview = Get-TestInstallPackageTargetSnapshot -TestRoot $testRoot
    $preview = ConvertFrom-TestInstallPackageSuccessfulCli -CliResult (Invoke-TestInstallPackageRecoveryCli -ShellPath $shellPath -RecoveryPath $recoveryCli -PluginDestination $recoveryPluginDestination -McpDestination $recoveryMcpDestination -OperationId ([string]$staged.operationId) -LogDirectory $logs) -Context 'Archived recovery preview'
    Assert-TestInstallTransaction -Condition ([string]$preview.mode -ceq 'preview' -and [bool]$preview.restoreAllowed -and [string]$preview.evidenceHash -match '^[0-9a-fA-F]{64}$') -Message 'Archived recovery preview did not issue a restore-bound evidence hash.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallPackageTargetSnapshot -TestRoot $testRoot) -ceq $recoveryBeforePreview) -Message 'Archived recovery preview changed a synthetic target, stage, or archive parent.'
    $restored = ConvertFrom-TestInstallPackageSuccessfulCli -CliResult (Invoke-TestInstallPackageRecoveryCli -ShellPath $shellPath -RecoveryPath $recoveryCli -PluginDestination $recoveryPluginDestination -McpDestination $recoveryMcpDestination -OperationId ([string]$staged.operationId) -LogDirectory $logs -RestoreOriginal -ExpectedEvidenceHash ([string]$preview.evidenceHash)) -Context 'Archived recovery restore'
    Assert-TestInstallTransaction -Condition ([bool]$restored.restoredOriginal -and [string]$restored.status -ceq 'rolled_back') -Message 'Archived recovery did not report a verified original-state restore.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root $recoveryPluginDestination) -ceq $partialPluginBefore) -Message 'Archived recovery did not restore the exact partial Plugin root and runtime-handoff sentinel.'
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $recoveryMcpDestination) -and -not (Test-Path -LiteralPath $recoveryMarkerPath)) -Message 'Archived recovery did not restore the originally absent MCP payload or remove its verified marker.'
    Assert-TestInstallPackagePairedRollback -TestRoot $testRoot -PluginStageParent (Split-Path -Parent $recoveryPluginStageRoot) -McpStageParent (Split-Path -Parent $recoveryMcpStageRoot) -OperationId ([string]$staged.operationId) -PluginDestination $recoveryPluginDestination -McpDestination $recoveryMcpDestination
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
        partialRootFirstInstallRecovery = 'archived-stage/internal-transaction-hook/archived-recovery-cli'
        realPackagedMcpProbe = $true
        gameLaunches = 0
    } | ConvertTo-Json -Compress
} finally {
    $resolvedTempParent = [IO.Path]::GetFullPath($testParent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $leaf = Split-Path -Leaf $resolvedTestRoot
    if (-not $resolvedTestRoot.StartsWith($resolvedTempParent, [StringComparison]::OrdinalIgnoreCase) -or
        $leaf -notmatch '^swip-[0-9a-f]{32}$') {
        throw "Refusing to clean an unexpected test path: $resolvedTestRoot"
    }
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
