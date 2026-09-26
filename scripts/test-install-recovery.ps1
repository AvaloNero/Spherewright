Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightInstallTransaction.ps1')
. (Join-Path $PSScriptRoot 'SpherewrightInstallRecovery.ps1')
. (Join-Path $PSScriptRoot 'SpherewrightInstallTestSupport.ps1')

function ConvertTo-TestInstallRecoveryPowerShellLiteral {
    param([Parameter(Mandatory)][string]$Value)
    return "'" + $Value.Replace("'", "''") + "'"
}

function Get-TestInstallRecoveryHostPath {
    $hostName = if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh.exe' } else { 'powershell.exe' }
    $candidate = Join-Path $PSHOME $hostName
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "The current PowerShell host executable was not found: $candidate"
    }
    return $candidate
}

function Get-TestInstallRecoveryOutputExcerpt {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    $text = [IO.File]::ReadAllText($Path)
    if ($text.Length -gt 4096) { return $text.Substring(0, 4096) + '...[truncated]' }
    return $text
}

function Invoke-TestInstallRecoveryCliRaw {
    param(
        [Parameter(Mandatory)][string]$CliPath,
        [Parameter(Mandatory)][string]$PluginDestination,
        [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$OperationId,
        [switch]$RestoreOriginal,
        [string]$ExpectedEvidenceHash
    )
    $arguments = @(
        '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $CliPath,
        '-PluginDestination', $PluginDestination,
        '-McpDestination', $McpDestination,
        '-OperationId', $OperationId
    )
    if ($RestoreOriginal) { $arguments += '-RestoreOriginal' }
    if ($PSBoundParameters.ContainsKey('ExpectedEvidenceHash')) {
        $arguments += '-ExpectedEvidenceHash'
        $arguments += $ExpectedEvidenceHash
    }
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        # Expected CLI rejections use stderr and a nonzero native exit code.
        # Capture those as test data instead of allowing this harness's Stop
        # preference to turn them into a parent-script exception.
        $ErrorActionPreference = 'Continue'
        $output = @(& (Get-TestInstallRecoveryHostPath) @arguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    return [pscustomobject]@{
        exitCode = $exitCode
        output = (@($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    }
}

function Invoke-TestInstallRecoveryCli {
    param(
        [Parameter(Mandatory)][string]$CliPath,
        [Parameter(Mandatory)][string]$PluginDestination,
        [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$OperationId,
        [switch]$RestoreOriginal,
        [string]$ExpectedEvidenceHash
    )
    $arguments = @{
        CliPath = $CliPath
        PluginDestination = $PluginDestination
        McpDestination = $McpDestination
        OperationId = $OperationId
    }
    if ($RestoreOriginal) { $arguments.RestoreOriginal = $true }
    if ($PSBoundParameters.ContainsKey('ExpectedEvidenceHash')) { $arguments.ExpectedEvidenceHash = $ExpectedEvidenceHash }
    $result = Invoke-TestInstallRecoveryCliRaw @arguments
    if ($result.exitCode -ne 0) {
        throw "Recovery CLI failed with exit code $($result.exitCode): $($result.output)"
    }
    try { return ($result.output | ConvertFrom-Json -ErrorAction Stop) }
    catch { throw "Recovery CLI did not return JSON: $($result.output)" }
}

function Invoke-TestInstallRecoveryCrashChild {
    param(
        [Parameter(Mandatory)][object]$Fixture,
        [Parameter(Mandatory)][object]$Pair,
        [Parameter(Mandatory)][string]$CrashStep
    )
    $childPath = Join-Path $Fixture.root 'crash-child.ps1'
    $sentinelPath = Join-Path $Fixture.root 'crash-step.txt'
    $stdoutPath = Join-Path $Fixture.root 'crash-child.stdout.txt'
    $stderrPath = Join-Path $Fixture.root 'crash-child.stderr.txt'
    $parentLiteral = if (@($Fixture.parentDirectories).Count -eq 0) {
        '@()'
    } else {
        '@(' + (@($Fixture.parentDirectories | ForEach-Object { ConvertTo-TestInstallRecoveryPowerShellLiteral ([string]$_) }) -join ', ') + ')'
    }
    $template = @'
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. __TRANSACTION_SCRIPT__
. __SUPPORT_SCRIPT__
$pluginExpected = Get-TestInstallTransactionExpectedFiles -Root __PLUGIN_PAYLOAD__
$mcpExpected = Get-TestInstallTransactionExpectedFiles -Root __MCP_PAYLOAD__
$parents = __PARENTS__
$crashStep = __CRASH_STEP__
$crashSentinel = __CRASH_SENTINEL__
$verifyInstalled = {
    param($liveMcpDirectory)
    return [pscustomobject]@{ metadata = 'unreachable-after-crash' }
}
$beforeMutation = {
    param($step)
    if ($step -ceq $crashStep) {
        [IO.File]::WriteAllText($crashSentinel, $step, [Text.UTF8Encoding]::new($false))
        [Environment]::FailFast('Synthetic explicit recovery interruption at ' + $step)
    }
}.GetNewClosure()
$null = Invoke-SpherewrightInstallTransaction `
    -PluginStagePayload __PLUGIN_PAYLOAD__ `
    -PluginDestination __PLUGIN_DESTINATION__ `
    -PluginExpectedFiles $pluginExpected `
    -McpStagePayload __MCP_PAYLOAD__ `
    -McpDestination __MCP_DESTINATION__ `
    -McpExpectedFiles $mcpExpected `
    -VerifyInstalled $verifyInstalled `
    -ParentDirectoriesToCreate $parents `
    -BeforeMutation $beforeMutation
throw 'The synthetic child reached the end without its crash hook.'
'@
    $content = $template
    foreach ($replacement in @{
        '__TRANSACTION_SCRIPT__' = ConvertTo-TestInstallRecoveryPowerShellLiteral (Join-Path $PSScriptRoot 'SpherewrightInstallTransaction.ps1')
        '__SUPPORT_SCRIPT__' = ConvertTo-TestInstallRecoveryPowerShellLiteral (Join-Path $PSScriptRoot 'SpherewrightInstallTestSupport.ps1')
        '__PLUGIN_PAYLOAD__' = ConvertTo-TestInstallRecoveryPowerShellLiteral $Pair.pluginPayload
        '__MCP_PAYLOAD__' = ConvertTo-TestInstallRecoveryPowerShellLiteral $Pair.mcpPayload
        '__PLUGIN_DESTINATION__' = ConvertTo-TestInstallRecoveryPowerShellLiteral $Fixture.pluginDestination
        '__MCP_DESTINATION__' = ConvertTo-TestInstallRecoveryPowerShellLiteral $Fixture.mcpDestination
        '__PARENTS__' = $parentLiteral
        '__CRASH_STEP__' = ConvertTo-TestInstallRecoveryPowerShellLiteral $CrashStep
        '__CRASH_SENTINEL__' = ConvertTo-TestInstallRecoveryPowerShellLiteral $sentinelPath
    }.GetEnumerator()) {
        $content = $content.Replace([string]$replacement.Key, [string]$replacement.Value)
    }
    [IO.File]::WriteAllText($childPath, $content, [Text.UTF8Encoding]::new($false))

    $process = $null
    try {
        $process = Start-Process -FilePath (Get-TestInstallRecoveryHostPath) `
            -ArgumentList ('-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + $childPath + '"') `
            -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath `
            -WindowStyle Hidden -PassThru
        if (-not $process.WaitForExit(20000)) {
            if (-not $process.HasExited) {
                # This is the exact child created above; no process discovery or
                # name-based termination is used by the synthetic test.
                $process.Kill()
                $process.WaitForExit()
            }
            throw 'Synthetic interruption child did not exit within 20 seconds.'
        }
        $exitCode = $process.ExitCode
    } finally {
        if ($null -ne $process) { $process.Dispose() }
    }
    if (-not (Test-Path -LiteralPath $sentinelPath -PathType Leaf)) {
        throw "Synthetic child did not reach the requested mutation step $CrashStep. stderr=$(Get-TestInstallRecoveryOutputExcerpt $stderrPath)"
    }
    if ([IO.File]::ReadAllText($sentinelPath) -cne $CrashStep) {
        throw 'Synthetic child crash sentinel does not identify the requested mutation step.'
    }
    if ($exitCode -eq 0) {
        throw "Synthetic interruption child unexpectedly exited successfully. stderr=$(Get-TestInstallRecoveryOutputExcerpt $stderrPath)"
    }
    return [pscustomobject]@{ exitCode=$exitCode; stderr=(Get-TestInstallRecoveryOutputExcerpt $stderrPath) }
}

function Assert-TestInstallRecoveryPairedRolledBack {
    param([Parameter(Mandatory)][object]$Fixture, [Parameter(Mandatory)][object]$Pair)
    foreach ($entry in @(
        [pscustomobject]@{parent=$Fixture.pluginStageParent;role='plugin'},
        [pscustomobject]@{parent=$Fixture.mcpStageParent;role='mcp'}
    )) {
        $archive = Join-Path $entry.parent ('.spherewright-archive-' + $Pair.operationId + '-' + $entry.role)
        $recordPath = Join-Path $archive 'transaction\progress.json'
        Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $recordPath -PathType Leaf) -Message "Explicit recovery did not retain the $($entry.role) terminal record."
        $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json -ErrorAction Stop
        Assert-TestInstallTransaction -Condition ([string]$record.status -ceq 'rolled_back') -Message "Explicit recovery did not record the $($entry.role) archive as rolled_back."
    }
}

function Assert-TestInstallRecoveryPreviewRejectedWithoutWrites {
    param(
        [Parameter(Mandatory)][string]$CliPath,
        [Parameter(Mandatory)][object]$Fixture,
        [Parameter(Mandatory)][object]$Pair,
        [Parameter(Mandatory)][string]$Context
    )
    $before = Get-TestInstallTransactionTree -Root $Fixture.root
    $rejected = $false
    try {
        Invoke-TestInstallRecoveryCli -CliPath $CliPath -PluginDestination $Fixture.pluginDestination -McpDestination $Fixture.mcpDestination -OperationId $Pair.operationId | Out-Null
    } catch { $rejected = $true }
    Assert-TestInstallTransaction -Condition $rejected -Message "Recovery preview accepted invalid evidence: $Context"
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root $Fixture.root) -ceq $before) -Message "Rejected recovery preview modified evidence: $Context"
}

function Test-InstallRecoveryInterruptedOperation {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Label,
        [switch]$FirstInstall,
        [switch]$ExerciseRecoveryFailure
    )
    $fixture = New-TestInstallTransactionFixture -Root (Join-Path $Root $Label) -FirstInstall:$FirstInstall -SkipPluginPayload:$FirstInstall
    $pair = $fixture.pair
    if ($FirstInstall) {
        Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $fixture.pluginDestination)) -Message 'Recovery first-install fixture unexpectedly has a pre-existing Plugin root.'
        Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $fixture.mcpDestination)) -Message 'Recovery first-install fixture unexpectedly has a pre-existing MCP root.'
    }
    $pluginBefore = Get-TestInstallTransactionSnapshot -Root $fixture.pluginDestination -ExpectedFiles $pair.pluginExpected
    $pluginDependenciesBefore = Get-TestInstallTransactionSnapshot -Root $fixture.pluginDestination -ExpectedFiles $pair.pluginExpected -ExcludeRelativePaths @('Spherewright.Plugin.dll')
    $mcpBefore = Get-TestInstallTransactionSnapshot -Root $fixture.mcpDestination -ExpectedFiles $pair.mcpExpected
    $handoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $fixture.pluginDestination 'runtime-handoff')
    $oldMainPath = Join-Path $fixture.pluginDestination 'Spherewright.Plugin.dll'
    $oldMainHash = if (Test-Path -LiteralPath $oldMainPath -PathType Leaf) { (Get-FileHash -LiteralPath $oldMainPath -Algorithm SHA256).Hash } else { $null }
    $crashStep = if ($FirstInstall) { 'copy-plugin-main-last' } else { 'progress-verify-installed' }
    $null = Invoke-TestInstallRecoveryCrashChild -Fixture $fixture -Pair $pair -CrashStep $crashStep

    $markerPath = Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $fixture.pluginStageParent
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $markerPath -PathType Leaf) -Message 'Synthetic interruption did not leave a pending startup marker for explicit recovery.'
    $beforePreview = Get-TestInstallTransactionTree -Root $fixture.root
    $cli = Join-Path $PSScriptRoot 'recover-install.ps1'
    $preview = Invoke-TestInstallRecoveryCli -CliPath $cli -PluginDestination $fixture.pluginDestination -McpDestination $fixture.mcpDestination -OperationId $pair.operationId
    Assert-TestInstallTransaction -Condition ([string]$preview.mode -ceq 'preview' -and [bool]$preview.restoreAllowed -and -not [bool]$preview.writesPerformed) -Message 'Recovery preview did not return the expected read-only approval shape.'
    Assert-TestInstallTransaction -Condition ([string]$preview.evidenceHash -match '^[0-9a-f]{64}$') -Message 'Recovery preview did not return a canonical evidence hash.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root $fixture.root) -ceq $beforePreview) -Message 'Recovery preview modified synthetic installation evidence.'

    if (-not $FirstInstall) {
        # These five negative cases reuse the same interrupted upgrade. They
        # alter only synthetic files, prove that rejection is write-free, then
        # put the exact bytes back before the approved restore below.
        $pluginStageRoot = Split-Path -Parent $pair.pluginPayload
        $backupContracts = Join-Path $pluginStageRoot 'transaction\old-payload\Spherewright.Contracts.dll'
        $temporarilyHiddenBackup = Join-Path $fixture.root 'hidden-old-contracts.dll'
        [IO.File]::Move($backupContracts, $temporarilyHiddenBackup)
        try {
            Assert-TestInstallRecoveryPreviewRejectedWithoutWrites -CliPath $cli -Fixture $fixture -Pair $pair -Context 'missing old backup file'
        } finally {
            if (Test-Path -LiteralPath $temporarilyHiddenBackup -PathType Leaf) { [IO.File]::Move($temporarilyHiddenBackup, $backupContracts) }
        }

        $liveContracts = Join-Path $fixture.pluginDestination 'Spherewright.Contracts.dll'
        $newContractsBytes = [IO.File]::ReadAllBytes($liveContracts)
        [IO.File]::WriteAllText($liveContracts, 'synthetic unknown live contract', [Text.UTF8Encoding]::new($false))
        try {
            Assert-TestInstallRecoveryPreviewRejectedWithoutWrites -CliPath $cli -Fixture $fixture -Pair $pair -Context 'unknown live non-main payload'
        } finally {
            [IO.File]::WriteAllBytes($liveContracts, $newContractsBytes)
        }

        $oldContractsBytes = [IO.File]::ReadAllBytes($backupContracts)
        [IO.File]::WriteAllBytes($liveContracts, $oldContractsBytes)
        try {
            $beforeStaleEvidenceRestore = Get-TestInstallTransactionTree -Root $fixture.root
            $staleEvidenceRejected = $false
            try {
                Invoke-TestInstallRecoveryCli -CliPath $cli -PluginDestination $fixture.pluginDestination -McpDestination $fixture.mcpDestination -OperationId $pair.operationId -RestoreOriginal -ExpectedEvidenceHash ([string]$preview.evidenceHash) | Out-Null
            } catch { $staleEvidenceRejected = $true }
            Assert-TestInstallTransaction -Condition $staleEvidenceRejected -Message 'Recovery accepted a stale preview after legal old/new live payload drift.'
            Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root $fixture.root) -ceq $beforeStaleEvidenceRestore) -Message 'A stale-evidence recovery rejection modified synthetic installation evidence.'
        } finally {
            [IO.File]::WriteAllBytes($liveContracts, $newContractsBytes)
        }

        $mcpStageRoot = Split-Path -Parent $pair.mcpPayload
        $mcpProgressPath = Join-Path $mcpStageRoot 'transaction\progress.json'
        $originalMcpProgressBytes = [IO.File]::ReadAllBytes($mcpProgressPath)
        $mcpProgress = [IO.File]::ReadAllText($mcpProgressPath, [Text.UTF8Encoding]::new($false, $true)) | ConvertFrom-Json -ErrorAction Stop
        $mirroredContract = @($mcpProgress.pluginExpectedFiles | Where-Object { [string]$_.relative -ceq 'Spherewright.Contracts.dll' })
        Assert-TestInstallTransaction -Condition ($mirroredContract.Count -eq 1) -Message 'Synthetic mirrored recovery record lacks the expected Plugin contract entry.'
        $mirroredContract[0].sha256 = ('1' * 64)
        [IO.File]::WriteAllText($mcpProgressPath, ($mcpProgress | ConvertTo-Json -Depth 16), [Text.UTF8Encoding]::new($false))
        try {
            Assert-TestInstallRecoveryPreviewRejectedWithoutWrites -CliPath $cli -Fixture $fixture -Pair $pair -Context 'mirrored immutable record mismatch'
        } finally {
            [IO.File]::WriteAllBytes($mcpProgressPath, $originalMcpProgressBytes)
        }

        $handoffDriftPath = Join-Path $fixture.pluginDestination 'runtime-handoff\synthetic-drift.txt'
        Write-TestInstallTransactionFile -Path $handoffDriftPath -Content 'synthetic protected handoff drift'
        try {
            Assert-TestInstallRecoveryPreviewRejectedWithoutWrites -CliPath $cli -Fixture $fixture -Pair $pair -Context 'protected runtime-handoff drift'
        } finally {
            if (Test-Path -LiteralPath $handoffDriftPath -PathType Leaf) { [IO.File]::Delete($handoffDriftPath) }
        }

        $preview = Invoke-TestInstallRecoveryCli -CliPath $cli -PluginDestination $fixture.pluginDestination -McpDestination $fixture.mcpDestination -OperationId $pair.operationId
        Assert-TestInstallTransaction -Condition ([string]$preview.mode -ceq 'preview' -and [bool]$preview.restoreAllowed) -Message 'Recovery did not issue a fresh preview after restored synthetic bytes.'
    }

    if ($ExerciseRecoveryFailure) {
        $recoveryFailure = [pscustomobject]@{ fired=$false; oldMainWasRestored=$false }
        $recoveryFailureHook = {
            param($step)
            if ($step -ceq 'progress-finalizing-explicit-recovery') {
                $recoveryFailure.fired = $true
                if (Test-Path -LiteralPath $oldMainPath -PathType Leaf) {
                    $recoveryFailure.oldMainWasRestored = ((Get-FileHash -LiteralPath $oldMainPath -Algorithm SHA256).Hash -ieq $oldMainHash)
                }
                throw 'Synthetic explicit-recovery finalization failure.'
            }
        }.GetNewClosure()
        $recoveryFailureThrew = $false
        try {
            Invoke-SpherewrightInstallRecovery -PluginDestination $fixture.pluginDestination -McpDestination $fixture.mcpDestination -OperationId $pair.operationId -ExpectedEvidenceHash ([string]$preview.evidenceHash) -BeforeMutation $recoveryFailureHook | Out-Null
        } catch { $recoveryFailureThrew = $true }
        Assert-TestInstallTransaction -Condition ($recoveryFailureThrew -and $recoveryFailure.fired -and $recoveryFailure.oldMainWasRestored) -Message 'Synthetic explicit-recovery failure did not occur after the old main DLL was restored.'
        Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $markerPath -PathType Leaf) -Message 'A failed explicit recovery removed its pending marker.'
        Assert-TestInstallTransactionOldDependenciesAndMcpRestoredWithMainWithheld -Fixture $fixture -Pair $pair -PluginBefore $pluginDependenciesBefore -McpBefore $mcpBefore -HandoffBefore $handoffBefore -Context 'explicit-recovery-finalization-failure'
        $recoveryFailureRecordPath = Join-Path (Split-Path -Parent $pair.pluginPayload) 'transaction\progress.json'
        $recoveryFailureRecord = Get-Content -LiteralPath $recoveryFailureRecordPath -Raw | ConvertFrom-Json -ErrorAction Stop
        Assert-TestInstallTransaction -Condition ([string]$recoveryFailureRecord.status -ceq 'needs_recovery') -Message 'A failed explicit recovery did not retain a needs_recovery record.'
        $preview = Invoke-TestInstallRecoveryCli -CliPath $cli -PluginDestination $fixture.pluginDestination -McpDestination $fixture.mcpDestination -OperationId $pair.operationId
        Assert-TestInstallTransaction -Condition ([string]$preview.mode -ceq 'preview' -and [bool]$preview.restoreAllowed) -Message 'Recovery did not issue a fresh preview after its failed attempt.'
    }

    $wrongEvidenceRejected = $false
    $beforeWrongEvidenceHash = Get-TestInstallTransactionTree -Root $fixture.root
    try {
        Invoke-TestInstallRecoveryCli -CliPath $cli -PluginDestination $fixture.pluginDestination -McpDestination $fixture.mcpDestination -OperationId $pair.operationId -RestoreOriginal -ExpectedEvidenceHash ('0' * 64) | Out-Null
    } catch { $wrongEvidenceRejected = $true }
    Assert-TestInstallTransaction -Condition $wrongEvidenceRejected -Message 'Recovery accepted a non-matching preview evidence hash.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root $fixture.root) -ceq $beforeWrongEvidenceHash) -Message 'A rejected recovery hash modified synthetic installation evidence.'

    $restored = Invoke-TestInstallRecoveryCli -CliPath $cli -PluginDestination $fixture.pluginDestination -McpDestination $fixture.mcpDestination -OperationId $pair.operationId -RestoreOriginal -ExpectedEvidenceHash ([string]$preview.evidenceHash)
    Assert-TestInstallTransaction -Condition ([string]$restored.status -ceq 'rolled_back' -and [bool]$restored.restoredOriginal) -Message 'Explicit recovery did not report restored original payloads.'
    Assert-TestInstallTransactionOldPayloadsRestored -Fixture $fixture -Pair $pair -PluginBefore $pluginBefore -McpBefore $mcpBefore -HandoffBefore $handoffBefore
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) -Message 'Verified explicit recovery did not remove its pending startup marker.'
    Assert-TestInstallRecoveryPairedRolledBack -Fixture $fixture -Pair $pair
    if ($FirstInstall) {
        Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $fixture.pluginDestination)) -Message 'Explicit first-install recovery left a Plugin root behind.'
        Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $fixture.mcpDestination)) -Message 'Explicit first-install recovery left an MCP root behind.'
    }
}

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('swr-ir-' + [guid]::NewGuid().ToString('N'))
$testLeaf = Split-Path -Leaf $testRoot
if ($testLeaf -notmatch '^swr-ir-[0-9a-f]{32}$') { throw 'Refusing an unsafe synthetic recovery test root.' }
[void][IO.Directory]::CreateDirectory($testRoot)

try {
    $passed = 0
    $cliPath = Join-Path $PSScriptRoot 'recover-install.ps1'
    $missingHash = Invoke-TestInstallRecoveryCliRaw -CliPath $cliPath -PluginDestination 'C:\synthetic\plugin' -McpDestination 'C:\synthetic\mcp' -OperationId ('0' * 32) -RestoreOriginal
    Assert-TestInstallTransaction -Condition ($missingHash.exitCode -ne 0) -Message 'Recovery CLI accepted -RestoreOriginal without an evidence hash.'
    $hashWithoutRestore = Invoke-TestInstallRecoveryCliRaw -CliPath $cliPath -PluginDestination 'C:\synthetic\plugin' -McpDestination 'C:\synthetic\mcp' -OperationId ('0' * 32) -ExpectedEvidenceHash ('0' * 64)
    Assert-TestInstallTransaction -Condition ($hashWithoutRestore.exitCode -ne 0) -Message 'Recovery CLI accepted an evidence hash without -RestoreOriginal.'
    $passed++

    Test-InstallRecoveryInterruptedOperation -Root $testRoot -Label 'upgrade' -ExerciseRecoveryFailure
    $passed++
    Test-InstallRecoveryInterruptedOperation -Root $testRoot -Label 'first' -FirstInstall
    $passed++

    [pscustomobject]@{ passed=$passed; childInterruptions=2; root=$testRoot } | ConvertTo-Json -Compress
} finally {
    if (Test-Path -LiteralPath $testRoot) {
        $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
        $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedTestRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
            (Split-Path -Leaf $resolvedTestRoot) -notmatch '^swr-ir-[0-9a-f]{32}$') { throw 'Refusing unsafe synthetic recovery test cleanup.' }
        [IO.Directory]::Delete($resolvedTestRoot, $true)
    }
}
