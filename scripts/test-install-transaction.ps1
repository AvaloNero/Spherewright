Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightInstallTransaction.ps1')

function Assert-TestInstallTransaction {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )
    if (-not $Condition) { throw $Message }
}

function Write-TestInstallTransactionFile {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Content)
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    [IO.File]::WriteAllText($Path, $Content, [Text.UTF8Encoding]::new($false))
}

function Get-TestInstallTransactionExpectedFiles {
    param([Parameter(Mandatory)][string]$Root)
    $result = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    $directories = [Collections.Generic.Stack[object]]::new()
    $directories.Push([pscustomobject]@{ path=$Root; relative='' })
    while ($directories.Count -gt 0) {
        $current = $directories.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $current.path -Force)) {
            $relative = if ([string]::IsNullOrEmpty($current.relative)) { $item.Name } else { "$($current.relative)/$($item.Name)" }
            if ($item.PSIsContainer) {
                $directories.Push([pscustomobject]@{ path=$item.FullName; relative=$relative })
            } else {
                $result.Add($relative, (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash)
            }
        }
    }
    return $result
}

function Get-TestInstallTransactionSnapshot {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][object]$ExpectedFiles)
    $lines = [Collections.Generic.List[string]]::new()
    $rootItem = Get-Item -LiteralPath $Root -Force -ErrorAction SilentlyContinue
    $lines.Add('root=' + ($null -ne $rootItem))
    foreach ($relative in @($ExpectedFiles.Keys | Sort-Object)) {
        $path = Join-Path $Root ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $lines.Add("$relative=$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)")
        } else {
            $lines.Add("$relative=<missing>")
        }
    }
    return ($lines -join "`n")
}

function Get-TestInstallTransactionTree {
    param([Parameter(Mandatory)][string]$Root)
    $item = Get-Item -LiteralPath $Root -Force -ErrorAction SilentlyContinue
    if ($null -eq $item) { return '<missing>' }
    $entries = [Collections.Generic.List[string]]::new()
    $directories = [Collections.Generic.Stack[object]]::new()
    $directories.Push([pscustomobject]@{ path=$item.FullName; relative='' })
    while ($directories.Count -gt 0) {
        $current = $directories.Pop()
        foreach ($child in @(Get-ChildItem -LiteralPath $current.path -Force)) {
            $relative = if ([string]::IsNullOrEmpty($current.relative)) { $child.Name } else { "$($current.relative)/$($child.Name)" }
            if ($child.PSIsContainer) {
                $entries.Add("D|$relative")
                $directories.Push([pscustomobject]@{ path=$child.FullName; relative=$relative })
            } else {
                $entries.Add("F|$relative|$((Get-FileHash -LiteralPath $child.FullName -Algorithm SHA256).Hash)")
            }
        }
    }
    return (@($entries | Sort-Object) -join "`n")
}

function New-TestInstallTransactionStagePair {
    param([Parameter(Mandatory)][object]$Fixture, [Parameter(Mandatory)][string]$Label)
    $operationId = [guid]::NewGuid().ToString('N')
    $pluginRoot = Join-Path $Fixture.pluginStageParent ('.spherewright-stage-' + $operationId + '-plugin')
    $mcpRoot = Join-Path $Fixture.mcpStageParent ('.spherewright-stage-' + $operationId + '-mcp')
    $pluginPayload = Join-Path $pluginRoot 'payload'
    $mcpPayload = Join-Path $mcpRoot 'payload'
    foreach ($name in @('Spherewright.Plugin.dll', 'Spherewright.Contracts.dll', 'Spherewright.Bridge.Core.dll', 'Newtonsoft.Json.dll')) {
        Write-TestInstallTransactionFile -Path (Join-Path $pluginPayload $name) -Content "$Label plugin $name"
    }
    Write-TestInstallTransactionFile -Path (Join-Path $mcpPayload 'Spherewright.Mcp.exe') -Content "$Label mcp executable"
    Write-TestInstallTransactionFile -Path (Join-Path $mcpPayload 'support\managed.dll') -Content "$Label mcp support"
    return [pscustomobject]@{
        operationId = $operationId
        pluginPayload = $pluginPayload
        mcpPayload = $mcpPayload
        pluginExpected = Get-TestInstallTransactionExpectedFiles -Root $pluginPayload
        mcpExpected = Get-TestInstallTransactionExpectedFiles -Root $mcpPayload
    }
}

function New-TestInstallTransactionFixture {
    param(
        [Parameter(Mandatory)][string]$Root,
        [switch]$FirstInstall,
        [string]$McpBaseOverride,
        [switch]$ReuseExistingMcp
    )
    $game = Join-Path $Root 'game'
    $pluginDestination = Join-Path $game 'BepInEx\plugins\Spherewright'
    $pluginStageParent = Join-Path $game 'BepInEx'
    $mcpBase = if ([string]::IsNullOrWhiteSpace($McpBaseOverride)) { Join-Path $Root 'mcp-base' } else { $McpBaseOverride }
    $mcpDestination = if ($FirstInstall) { Join-Path $mcpBase 'one\two\installed' } else { Join-Path $mcpBase 'installed' }
    [void][IO.Directory]::CreateDirectory($pluginStageParent)
    if (-not $ReuseExistingMcp) { [void][IO.Directory]::CreateDirectory($mcpBase) }
    [void][IO.Directory]::CreateDirectory($pluginDestination)
    Write-TestInstallTransactionFile -Path (Join-Path $pluginDestination 'Spherewright.Plugin.dll') -Content 'old plugin main'
    Write-TestInstallTransactionFile -Path (Join-Path $pluginDestination 'Spherewright.Contracts.dll') -Content 'old contracts'
    Write-TestInstallTransactionFile -Path (Join-Path $pluginDestination 'runtime-handoff\handoff.json') -Content 'handoff must remain unchanged'
    if (-not $FirstInstall -and -not $ReuseExistingMcp) {
        [void][IO.Directory]::CreateDirectory($mcpDestination)
        Write-TestInstallTransactionFile -Path (Join-Path $mcpDestination 'Spherewright.Mcp.exe') -Content 'old mcp executable'
    }
    $fixture = [pscustomobject]@{
        root = $Root
        game = $game
        pluginDestination = $pluginDestination
        pluginStageParent = $pluginStageParent
        mcpBase = $mcpBase
        mcpStageParent = $mcpBase
        mcpDestination = $mcpDestination
        parentDirectories = if ($FirstInstall) { @((Join-Path $mcpBase 'one'), (Join-Path $mcpBase 'one\two')) } else { @() }
    }
    $fixture | Add-Member -NotePropertyName pair -NotePropertyValue (New-TestInstallTransactionStagePair -Fixture $fixture -Label 'first')
    return $fixture
}

function Assert-TestInstallTransactionOldPayloadsRestored {
    param(
        [Parameter(Mandatory)][object]$Fixture,
        [Parameter(Mandatory)][object]$Pair,
        [Parameter(Mandatory)][string]$PluginBefore,
        [Parameter(Mandatory)][string]$McpBefore,
        [Parameter(Mandatory)][string]$HandoffBefore
    )
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionSnapshot -Root $Fixture.pluginDestination -ExpectedFiles $Pair.pluginExpected) -ceq $PluginBefore) -Message 'Caught failure did not restore the exact old Plugin payload set.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionSnapshot -Root $Fixture.mcpDestination -ExpectedFiles $Pair.mcpExpected) -ceq $McpBefore) -Message 'Caught failure did not restore the exact old MCP payload set.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root (Join-Path $Fixture.pluginDestination 'runtime-handoff')) -ceq $HandoffBefore) -Message 'Caught failure changed runtime-handoff.'
}

function Invoke-TestInstallTransaction {
    param(
        [Parameter(Mandatory)][object]$Fixture,
        [Parameter(Mandatory)][object]$Pair,
        [scriptblock]$VerifyInstalled,
        [scriptblock]$BeforeMutation
    )
    if ($null -eq $VerifyInstalled) {
        $VerifyInstalled = {
            param($liveMcpDirectory)
            if (-not (Test-Path -LiteralPath (Join-Path $liveMcpDirectory 'Spherewright.Mcp.exe') -PathType Leaf)) {
                throw 'Synthetic metadata probe could not find the promoted MCP executable.'
            }
            return [pscustomobject]@{ metadata = 'synthetic' }
        }
    }
    $arguments = @{
        PluginStagePayload = $Pair.pluginPayload
        PluginDestination = $Fixture.pluginDestination
        PluginExpectedFiles = $Pair.pluginExpected
        McpStagePayload = $Pair.mcpPayload
        McpDestination = $Fixture.mcpDestination
        McpExpectedFiles = $Pair.mcpExpected
        VerifyInstalled = $VerifyInstalled
        ParentDirectoriesToCreate = $Fixture.parentDirectories
    }
    if ($null -ne $BeforeMutation) { $arguments.BeforeMutation = $BeforeMutation }
    return Invoke-SpherewrightInstallTransaction @arguments
}

function Test-InstallTransactionMutationFaultCoverage {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Name,
        [switch]$FirstInstall
    )

    $traceFixture = New-TestInstallTransactionFixture -Root (Join-Path $Root ($Name + '-trace')) -FirstInstall:$FirstInstall
    $traceSteps = [Collections.Generic.List[string]]::new()
    $traceHook = {
        param($step)
        [void]$traceSteps.Add([string]$step)
    }.GetNewClosure()
    Invoke-TestInstallTransaction -Fixture $traceFixture -Pair $traceFixture.pair -BeforeMutation $traceHook | Out-Null
    $uniqueSteps = @($traceSteps | Select-Object -Unique)
    Assert-TestInstallTransaction -Condition ($uniqueSteps.Count -gt 0) -Message 'Synthetic transaction trace captured no mutations.'

    $caseCount = 0
    for ($stepIndex = 0; $stepIndex -lt $uniqueSteps.Count; $stepIndex++) {
        $faultStep = [string]$uniqueSteps[$stepIndex]
        $caseFixture = New-TestInstallTransactionFixture -Root (Join-Path $Root ($Name + '-fault-' + $stepIndex)) -FirstInstall:$FirstInstall
        $pluginBefore = Get-TestInstallTransactionSnapshot -Root $caseFixture.pluginDestination -ExpectedFiles $caseFixture.pair.pluginExpected
        $mcpBefore = Get-TestInstallTransactionSnapshot -Root $caseFixture.mcpDestination -ExpectedFiles $caseFixture.pair.mcpExpected
        $handoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $caseFixture.pluginDestination 'runtime-handoff')
        $fault = [pscustomobject]@{ fired = $false }
        $faultHook = {
            param($step)
            if (-not $fault.fired -and $step -ceq $faultStep) {
                $fault.fired = $true
                throw ('Synthetic mutation failure: ' + $faultStep)
            }
        }.GetNewClosure()
        $threw = $false
        try {
            Invoke-TestInstallTransaction -Fixture $caseFixture -Pair $caseFixture.pair -BeforeMutation $faultHook | Out-Null
        } catch {
            $threw = $true
        }
        Assert-TestInstallTransaction -Condition ($threw -and $fault.fired) -Message "Mutation fault was not surfaced: $faultStep"
        Assert-TestInstallTransactionOldPayloadsRestored -Fixture $caseFixture -Pair $caseFixture.pair -PluginBefore $pluginBefore -McpBefore $mcpBefore -HandoffBefore $handoffBefore
        $caseCount++
    }
    return $caseCount
}

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('spherewright-install-transaction-tests-' + [guid]::NewGuid().ToString('N'))
if ((Split-Path -Leaf $testRoot) -notmatch '^spherewright-install-transaction-tests-[0-9a-f]{32}$') { throw 'Unsafe synthetic test cleanup root.' }
[void][IO.Directory]::CreateDirectory($testRoot)
$passed = 0
$faultCases = 0
try {
    $successFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'success')
    $handoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $successFixture.pluginDestination 'runtime-handoff')
    $result = Invoke-TestInstallTransaction -Fixture $successFixture -Pair $successFixture.pair
    Assert-TestInstallTransaction -Condition ($result.installed -and $result.status -ceq 'committed' -and $result.caughtFailureRollbackSupported -and -not $result.crashRecoverySupported -and -not $result.transactionalUpgrade) -Message 'Successful transaction returned unsafe or incomplete evidence flags.'
    $null = Assert-SpherewrightInstallSnapshotComplete -Root $successFixture.pluginDestination -ExpectedFiles $successFixture.pair.pluginExpected -PreservedDirectories @('runtime-handoff')
    $null = Assert-SpherewrightInstallSnapshotComplete -Root $successFixture.mcpDestination -ExpectedFiles $successFixture.pair.mcpExpected
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root (Join-Path $successFixture.pluginDestination 'runtime-handoff')) -ceq $handoffBefore) -Message 'Successful transaction changed runtime-handoff.'
    foreach ($recordPath in @($result.recordPath, (Join-Path $result.mcpArchive 'transaction\progress.json'))) {
        $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
        Assert-TestInstallTransaction -Condition ([string]$record.status -ceq 'committed') -Message 'Successful transaction did not mirror a committed progress record.'
    }
    Assert-SpherewrightInstallNoPendingArchives -Parent $successFixture.pluginStageParent
    Assert-SpherewrightInstallNoPendingArchives -Parent $successFixture.mcpStageParent
    $passed++

    # A terminal pair of mirrored archive records must allow a normal
    # reinstall, rather than permanently reserving either staging parent.
    $secondPair = New-TestInstallTransactionStagePair -Fixture $successFixture -Label 'second'
    $secondResult = Invoke-TestInstallTransaction -Fixture $successFixture -Pair $secondPair
    Assert-TestInstallTransaction -Condition ($secondResult.installed -and $secondResult.status -ceq 'committed') -Message 'A reinstall after a committed transaction was rejected by the residue gate.'
    $passed++

    $firstInstallFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'first-install') -FirstInstall
    $firstInstallResult = Invoke-TestInstallTransaction -Fixture $firstInstallFixture -Pair $firstInstallFixture.pair
    $firstInstallRecord = Get-Content -LiteralPath $firstInstallResult.recordPath -Raw | ConvertFrom-Json
    Assert-TestInstallTransaction -Condition (@($firstInstallRecord.plannedParentDirectories).Count -eq 2 -and @($firstInstallRecord.createdParentDirectories).Count -eq 2) -Message 'First-install parent creation was not journaled exactly.'
    foreach ($parent in @($firstInstallFixture.parentDirectories)) {
        Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $parent -PathType Container) -Message 'First-install MCP parent was not created after backup.'
    }
    $passed++

    $callbackFailureFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'callback-failure')
    $callbackPluginBefore = Get-TestInstallTransactionSnapshot -Root $callbackFailureFixture.pluginDestination -ExpectedFiles $callbackFailureFixture.pair.pluginExpected
    $callbackMcpBefore = Get-TestInstallTransactionSnapshot -Root $callbackFailureFixture.mcpDestination -ExpectedFiles $callbackFailureFixture.pair.mcpExpected
    $callbackHandoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $callbackFailureFixture.pluginDestination 'runtime-handoff')
    $callbackQuiesce = [pscustomobject]@{ mainWasPresentDuringDependencyRestore = $false }
    $callbackHook = {
        param($step)
        if ($step -like 'rollback-copy-plugin-old-*' -or $step -like 'rollback-move-plugin-new-Spherewright.Contracts.dll') {
            if (Test-Path -LiteralPath (Join-Path $callbackFailureFixture.pluginDestination 'Spherewright.Plugin.dll') -PathType Leaf) {
                $callbackQuiesce.mainWasPresentDuringDependencyRestore = $true
            }
        }
    }.GetNewClosure()
    $callbackThrew = $false
    try {
        Invoke-TestInstallTransaction -Fixture $callbackFailureFixture -Pair $callbackFailureFixture.pair -VerifyInstalled { param($liveMcpDirectory) throw 'Synthetic post-promotion metadata failure.' } -BeforeMutation $callbackHook | Out-Null
    } catch {
        $callbackThrew = $true
    }
    Assert-TestInstallTransaction -Condition $callbackThrew -Message 'A failing post-promotion metadata callback did not fail the transaction.'
    Assert-TestInstallTransactionOldPayloadsRestored -Fixture $callbackFailureFixture -Pair $callbackFailureFixture.pair -PluginBefore $callbackPluginBefore -McpBefore $callbackMcpBefore -HandoffBefore $callbackHandoffBefore
    Assert-TestInstallTransaction -Condition (-not $callbackQuiesce.mainWasPresentDuringDependencyRestore) -Message 'Rollback restored Plugin dependencies while the promoted main Plugin DLL remained live.'
    Assert-SpherewrightInstallNoPendingArchives -Parent $callbackFailureFixture.pluginStageParent
    Assert-SpherewrightInstallNoPendingArchives -Parent $callbackFailureFixture.mcpStageParent
    $passed++

    $rollbackFaultFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'rollback-dependency-fault')
    $rollbackFaultObserved = [pscustomobject]@{ mainWasAbsent = $false; fired = $false }
    $rollbackFaultHook = {
        param($step)
        if (-not $rollbackFaultObserved.fired -and $step -ceq 'rollback-copy-plugin-old-Spherewright.Contracts.dll') {
            $rollbackFaultObserved.fired = $true
            $rollbackFaultObserved.mainWasAbsent = -not (Test-Path -LiteralPath (Join-Path $rollbackFaultFixture.pluginDestination 'Spherewright.Plugin.dll') -PathType Leaf)
            throw 'Synthetic dependency rollback failure.'
        }
    }.GetNewClosure()
    $rollbackFaultThrew = $false
    try {
        Invoke-TestInstallTransaction -Fixture $rollbackFaultFixture -Pair $rollbackFaultFixture.pair -VerifyInstalled { param($liveMcpDirectory) throw 'Synthetic post-promotion metadata failure.' } -BeforeMutation $rollbackFaultHook | Out-Null
    } catch {
        $rollbackFaultThrew = $true
    }
    Assert-TestInstallTransaction -Condition ($rollbackFaultThrew -and $rollbackFaultObserved.fired -and $rollbackFaultObserved.mainWasAbsent) -Message 'A dependency rollback failure did not quiesce the promoted main Plugin DLL first.'
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Join-Path $rollbackFaultFixture.pluginDestination 'Spherewright.Plugin.dll') -PathType Leaf)) -Message 'A failed dependency rollback left the promoted main Plugin DLL live.'
    $rollbackFaultRecord = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $rollbackFaultFixture.pair.pluginPayload) 'transaction\progress.json') -Raw | ConvertFrom-Json
    Assert-TestInstallTransaction -Condition ([string]$rollbackFaultRecord.status -ceq 'needs_recovery') -Message 'A failed dependency rollback did not preserve needs_recovery evidence.'
    $passed++

    # Lock the second mirrored journal before the terminal write. Despite the
    # persistent recording failure, old payload restoration must be attempted.
    # Residue must block reuse. The separate installer integration suite tests
    # a deliberately split terminal/nonterminal pair from its terminal side.
    $mirrorFailureFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'mirror-write-failure')
    $mirrorPluginBefore = Get-TestInstallTransactionSnapshot -Root $mirrorFailureFixture.pluginDestination -ExpectedFiles $mirrorFailureFixture.pair.pluginExpected
    $mirrorMcpBefore = Get-TestInstallTransactionSnapshot -Root $mirrorFailureFixture.mcpDestination -ExpectedFiles $mirrorFailureFixture.pair.mcpExpected
    $mirrorHandoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $mirrorFailureFixture.pluginDestination 'runtime-handoff')
    $mirrorLock = [pscustomobject]@{ fired = $false; stream = $null }
    $mirrorHook = {
        param($step)
        if (-not $mirrorLock.fired -and $step -ceq 'progress-committed') {
            $mirrorLock.fired = $true
            $archive = Join-Path $mirrorFailureFixture.mcpStageParent ('.spherewright-archive-' + $mirrorFailureFixture.pair.operationId + '-mcp')
            $mirrorLock.stream = [IO.File]::Open((Join-Path $archive 'transaction\progress.json'), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
        }
    }.GetNewClosure()
    $mirrorThrew = $false
    try {
        Invoke-TestInstallTransaction -Fixture $mirrorFailureFixture -Pair $mirrorFailureFixture.pair -BeforeMutation $mirrorHook | Out-Null
    } catch {
        $mirrorThrew = $true
    } finally {
        if ($null -ne $mirrorLock.stream) { $mirrorLock.stream.Dispose() }
    }
    Assert-TestInstallTransaction -Condition ($mirrorThrew -and $mirrorLock.fired) -Message 'The synthetic second journal mirror failure did not surface.'
    Assert-TestInstallTransactionOldPayloadsRestored -Fixture $mirrorFailureFixture -Pair $mirrorFailureFixture.pair -PluginBefore $mirrorPluginBefore -McpBefore $mirrorMcpBefore -HandoffBefore $mirrorHandoffBefore
    $archiveGateRejected = $false
    try { Assert-SpherewrightInstallNoPendingArchives -Parent $mirrorFailureFixture.mcpStageParent } catch { $archiveGateRejected = $true }
    Assert-TestInstallTransaction -Condition $archiveGateRejected -Message 'A single-side terminal archive record bypassed the counterpart residue gate.'
    $alternateFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'mirror-other-plugin') -McpBaseOverride $mirrorFailureFixture.mcpBase -ReuseExistingMcp
    $alternateMcpBefore = Get-TestInstallTransactionSnapshot -Root $alternateFixture.mcpDestination -ExpectedFiles $alternateFixture.pair.mcpExpected
    $alternateThrew = $false
    try { Invoke-TestInstallTransaction -Fixture $alternateFixture -Pair $alternateFixture.pair | Out-Null } catch { $alternateThrew = $true }
    Assert-TestInstallTransaction -Condition $alternateThrew -Message 'A different Plugin target bypassed a non-terminal shared MCP archive counterpart.'
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionSnapshot -Root $alternateFixture.mcpDestination -ExpectedFiles $alternateFixture.pair.mcpExpected) -ceq $alternateMcpBefore) -Message 'The rejected shared-MCP transaction changed the live MCP payload.'
    $passed++

    # Keep the many fault fixtures shallow: Windows PowerShell/.NET Framework
    # filesystem APIs otherwise approach MAX_PATH while preserving archives.
    $faultCases += Test-InstallTransactionMutationFaultCoverage -Root $testRoot -Name 'u'
    $faultCases += Test-InstallTransactionMutationFaultCoverage -Root $testRoot -Name 'i' -FirstInstall
    Assert-TestInstallTransaction -Condition ($faultCases -gt 0) -Message 'Synthetic mutation fault coverage did not run.'
    $passed++

    [pscustomobject]@{ passed=$passed; faultCases=$faultCases; root=$testRoot } | ConvertTo-Json -Compress
} finally {
    if (Test-Path -LiteralPath $testRoot) {
        $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
        $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedTestRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
            (Split-Path -Leaf $resolvedTestRoot) -notmatch '^spherewright-install-transaction-tests-[0-9a-f]{32}$') { throw 'Refusing unsafe synthetic test cleanup.' }
        [IO.Directory]::Delete($resolvedTestRoot, $true)
    }
}
