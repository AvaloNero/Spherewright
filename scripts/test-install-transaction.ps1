Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightInstallTransaction.ps1')

. (Join-Path $PSScriptRoot 'SpherewrightInstallTestSupport.ps1')
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
        $pluginDependenciesBefore = Get-TestInstallTransactionSnapshot -Root $caseFixture.pluginDestination -ExpectedFiles $caseFixture.pair.pluginExpected -ExcludeRelativePaths @('Spherewright.Plugin.dll')
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
        if ($faultStep -ceq 'delete-pending-marker') {
            $markerPath = Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $caseFixture.pluginStageParent
            Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath $markerPath -PathType Leaf) -Message 'A failed pending-marker delete did not preserve the marker.'
            Assert-TestInstallTransactionPromotedDependenciesAndMcpWithMainWithheld -Fixture $caseFixture -Pair $caseFixture.pair -HandoffBefore $handoffBefore -Context $faultStep
        } else {
            Assert-TestInstallTransactionOldPayloadsRestored -Fixture $caseFixture -Pair $caseFixture.pair -PluginBefore $pluginBefore -McpBefore $mcpBefore -HandoffBefore $handoffBefore
        }
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
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $successFixture.pluginStageParent))) -Message 'A verified committed transaction left its pending startup marker behind.'
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
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $firstInstallFixture.pluginStageParent))) -Message 'A verified first install left its pending startup marker behind.'
    $passed++

    $blockedMarkerFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'bpm')
    $blockedMarkerPath = Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $blockedMarkerFixture.pluginStageParent
    Write-TestInstallTransactionFile -Path $blockedMarkerPath -Content '{"schemaVersion":1,"operationId":"manual"}'
    $blockedMarkerText = [IO.File]::ReadAllText($blockedMarkerPath)
    $blockedPluginBefore = Get-TestInstallTransactionSnapshot -Root $blockedMarkerFixture.pluginDestination -ExpectedFiles $blockedMarkerFixture.pair.pluginExpected
    $blockedMcpBefore = Get-TestInstallTransactionSnapshot -Root $blockedMarkerFixture.mcpDestination -ExpectedFiles $blockedMarkerFixture.pair.mcpExpected
    $blockedHandoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $blockedMarkerFixture.pluginDestination 'runtime-handoff')
    $blockedMarkerThrew = $false
    try { Invoke-TestInstallTransaction -Fixture $blockedMarkerFixture -Pair $blockedMarkerFixture.pair | Out-Null } catch { $blockedMarkerThrew = $true }
    Assert-TestInstallTransaction -Condition $blockedMarkerThrew -Message 'A pre-existing pending startup marker did not reject the transaction.'
    Assert-TestInstallTransaction -Condition ([IO.File]::ReadAllText($blockedMarkerPath) -ceq $blockedMarkerText) -Message 'A pre-existing pending startup marker was altered.'
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $blockedMarkerFixture.pair.pluginPayload) 'transaction'))) -Message 'A pre-existing pending marker was checked after transaction record creation.'
    Assert-TestInstallTransactionOldPayloadsRestored -Fixture $blockedMarkerFixture -Pair $blockedMarkerFixture.pair -PluginBefore $blockedPluginBefore -McpBefore $blockedMcpBefore -HandoffBefore $blockedHandoffBefore
    $passed++

    $markerWriteFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'fmw') -FirstInstall
    $markerWritePluginBefore = Get-TestInstallTransactionSnapshot -Root $markerWriteFixture.pluginDestination -ExpectedFiles $markerWriteFixture.pair.pluginExpected
    $markerWriteMcpBefore = Get-TestInstallTransactionSnapshot -Root $markerWriteFixture.mcpDestination -ExpectedFiles $markerWriteFixture.pair.mcpExpected
    $markerWriteHandoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $markerWriteFixture.pluginDestination 'runtime-handoff')
    $markerWriteHook = {
        param($step)
        if ($step -ceq 'write-pending-marker') { throw 'Synthetic pending-marker create failure.' }
    }
    $markerWriteThrew = $false
    try { Invoke-TestInstallTransaction -Fixture $markerWriteFixture -Pair $markerWriteFixture.pair -BeforeMutation $markerWriteHook | Out-Null } catch { $markerWriteThrew = $true }
    Assert-TestInstallTransaction -Condition $markerWriteThrew -Message 'A pending-marker create failure did not stop the transaction.'
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $markerWriteFixture.pluginStageParent))) -Message 'A pre-write marker failure left an orphan pending marker claim.'
    foreach ($parent in @($markerWriteFixture.parentDirectories)) {
        Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath $parent)) -Message 'A pending-marker write failure created a first-install MCP parent.'
    }
    Assert-TestInstallTransactionOldPayloadsRestored -Fixture $markerWriteFixture -Pair $markerWriteFixture.pair -PluginBefore $markerWritePluginBefore -McpBefore $markerWriteMcpBefore -HandoffBefore $markerWriteHandoffBefore
    $passed++

    $markerDeleteFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'md')
    $markerDeleteHandoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $markerDeleteFixture.pluginDestination 'runtime-handoff')
    $markerDeleteHook = {
        param($step)
        if ($step -ceq 'delete-pending-marker') { throw 'Synthetic pending-marker delete failure.' }
    }
    $markerDeleteThrew = $false
    try { Invoke-TestInstallTransaction -Fixture $markerDeleteFixture -Pair $markerDeleteFixture.pair -BeforeMutation $markerDeleteHook | Out-Null } catch { $markerDeleteThrew = $true }
    $markerDeletePath = Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $markerDeleteFixture.pluginStageParent
    Assert-TestInstallTransaction -Condition ($markerDeleteThrew -and (Test-Path -LiteralPath $markerDeletePath -PathType Leaf)) -Message 'A pending-marker delete failure did not preserve unresolved marker evidence.'
    Assert-TestInstallTransactionPromotedDependenciesAndMcpWithMainWithheld -Fixture $markerDeleteFixture -Pair $markerDeleteFixture.pair -HandoffBefore $markerDeleteHandoffBefore -Context 'explicit-marker-delete'
    $markerDeleteArchive = Join-Path $markerDeleteFixture.pluginStageParent ('.spherewright-archive-' + $markerDeleteFixture.pair.operationId + '-plugin')
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath (Join-Path $markerDeleteArchive 'transaction\failed-new\unresolved-live-main.dll') -PathType Leaf) -Message 'A pending-marker delete failure did not retain the withheld main DLL as operation evidence.'
    $markerDeleteRecord = Get-Content -LiteralPath (Join-Path $markerDeleteArchive 'transaction\progress.json') -Raw | ConvertFrom-Json
    Assert-TestInstallTransaction -Condition ([string]$markerDeleteRecord.status -ceq 'needs_recovery') -Message 'A pending-marker delete failure did not preserve a needs_recovery record.'
    $passed++

    # The deletion routine must re-read its identity after the mutation hook.
    # A replacement marker is external evidence and must remain in place rather
    # than being deleted as if it belonged to this operation.
    $markerIdentityFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'mit')
    $markerIdentityHandoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $markerIdentityFixture.pluginDestination 'runtime-handoff')
    $markerIdentityPath = Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $markerIdentityFixture.pluginStageParent
    $replacementMarkerOperationId = 'foreign-pending-marker'
    $markerIdentityHook = {
        param($step)
        if ($step -ceq 'delete-pending-marker') {
            $replacement = [IO.File]::ReadAllText($markerIdentityPath, [Text.UTF8Encoding]::new($false, $true)) | ConvertFrom-Json
            $replacement.operationId = $replacementMarkerOperationId
            [IO.File]::WriteAllText($markerIdentityPath, ($replacement | ConvertTo-Json -Depth 4), [Text.UTF8Encoding]::new($false))
        }
    }.GetNewClosure()
    $markerIdentityThrew = $false
    try { Invoke-TestInstallTransaction -Fixture $markerIdentityFixture -Pair $markerIdentityFixture.pair -BeforeMutation $markerIdentityHook | Out-Null } catch { $markerIdentityThrew = $true }
    Assert-TestInstallTransaction -Condition ($markerIdentityThrew -and (Test-Path -LiteralPath $markerIdentityPath -PathType Leaf)) -Message 'A replaced pending marker was deleted instead of being preserved as unresolved evidence.'
    $replacementMarker = Read-SpherewrightInstallPendingMarker -MarkerPath $markerIdentityPath
    Assert-TestInstallTransaction -Condition ([string]$replacementMarker.operationId -ceq $replacementMarkerOperationId) -Message 'The preserved pending marker was not the replacement identity.'
    Assert-TestInstallTransactionPromotedDependenciesAndMcpWithMainWithheld -Fixture $markerIdentityFixture -Pair $markerIdentityFixture.pair -HandoffBefore $markerIdentityHandoffBefore -Context 'marker-identity-replacement'
    $markerIdentityArchive = Join-Path $markerIdentityFixture.pluginStageParent ('.spherewright-archive-' + $markerIdentityFixture.pair.operationId + '-plugin')
    $markerIdentityRecord = Get-Content -LiteralPath (Join-Path $markerIdentityArchive 'transaction\progress.json') -Raw | ConvertFrom-Json
    Assert-TestInstallTransaction -Condition ([string]$markerIdentityRecord.status -ceq 'needs_recovery') -Message 'A replaced pending marker did not preserve a needs_recovery record.'
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath (Join-Path $markerIdentityArchive 'transaction\failed-new\unresolved-live-main.dll') -PathType Leaf) -Message 'A replaced pending marker did not retain the withheld main DLL as operation evidence.'
    $passed++

    $rollbackMarkerDeleteFixture = New-TestInstallTransactionFixture -Root (Join-Path $testRoot 'rmd')
    $rollbackMarkerPluginDependenciesBefore = Get-TestInstallTransactionSnapshot -Root $rollbackMarkerDeleteFixture.pluginDestination -ExpectedFiles $rollbackMarkerDeleteFixture.pair.pluginExpected -ExcludeRelativePaths @('Spherewright.Plugin.dll')
    $rollbackMarkerMcpBefore = Get-TestInstallTransactionSnapshot -Root $rollbackMarkerDeleteFixture.mcpDestination -ExpectedFiles $rollbackMarkerDeleteFixture.pair.mcpExpected
    $rollbackMarkerHandoffBefore = Get-TestInstallTransactionTree -Root (Join-Path $rollbackMarkerDeleteFixture.pluginDestination 'runtime-handoff')
    $rollbackMarkerDeleteHook = {
        param($step)
        if ($step -ceq 'delete-pending-marker') { throw 'Synthetic rollback pending-marker delete failure.' }
    }
    $rollbackMarkerDeleteThrew = $false
    $rollbackMarkerDeleteError = ''
    try { Invoke-TestInstallTransaction -Fixture $rollbackMarkerDeleteFixture -Pair $rollbackMarkerDeleteFixture.pair -VerifyInstalled { param($liveMcpDirectory) throw 'Synthetic post-promotion metadata failure.' } -BeforeMutation $rollbackMarkerDeleteHook | Out-Null } catch { $rollbackMarkerDeleteThrew = $true; $rollbackMarkerDeleteError = $_.Exception.Message }
    $rollbackMarkerDeletePath = Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $rollbackMarkerDeleteFixture.pluginStageParent
    Assert-TestInstallTransaction -Condition ($rollbackMarkerDeleteThrew -and (Test-Path -LiteralPath $rollbackMarkerDeletePath -PathType Leaf)) -Message 'A rollback marker-delete failure did not preserve unresolved marker evidence.'
    Assert-TestInstallTransactionOldDependenciesAndMcpRestoredWithMainWithheld -Fixture $rollbackMarkerDeleteFixture -Pair $rollbackMarkerDeleteFixture.pair -PluginBefore $rollbackMarkerPluginDependenciesBefore -McpBefore $rollbackMarkerMcpBefore -HandoffBefore $rollbackMarkerHandoffBefore -Context ('rollback-marker-delete ' + $rollbackMarkerDeleteError)
    $rollbackMarkerDeleteArchive = Join-Path $rollbackMarkerDeleteFixture.pluginStageParent ('.spherewright-archive-' + $rollbackMarkerDeleteFixture.pair.operationId + '-plugin')
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath (Join-Path $rollbackMarkerDeleteArchive 'transaction\failed-new\unresolved-live-main.dll') -PathType Leaf) -Message 'A rollback marker-delete failure did not retain the withheld old main DLL as operation evidence.'
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
    $mirrorPluginDependenciesBefore = Get-TestInstallTransactionSnapshot -Root $mirrorFailureFixture.pluginDestination -ExpectedFiles $mirrorFailureFixture.pair.pluginExpected -ExcludeRelativePaths @('Spherewright.Plugin.dll')
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
    $mirrorError = ''
    try {
        Invoke-TestInstallTransaction -Fixture $mirrorFailureFixture -Pair $mirrorFailureFixture.pair -BeforeMutation $mirrorHook | Out-Null
    } catch {
        $mirrorThrew = $true
        $mirrorError = $_.Exception.Message
    } finally {
        if ($null -ne $mirrorLock.stream) { $mirrorLock.stream.Dispose() }
    }
    Assert-TestInstallTransaction -Condition ($mirrorThrew -and $mirrorLock.fired) -Message 'The synthetic second journal mirror failure did not surface.'
    Assert-TestInstallTransactionOldDependenciesAndMcpRestoredWithMainWithheld -Fixture $mirrorFailureFixture -Pair $mirrorFailureFixture.pair -PluginBefore $mirrorPluginDependenciesBefore -McpBefore $mirrorMcpBefore -HandoffBefore $mirrorHandoffBefore -Context $mirrorError
    Assert-TestInstallTransaction -Condition (Test-Path -LiteralPath (Get-SpherewrightInstallPendingMarkerPath -PluginStageParent $mirrorFailureFixture.pluginStageParent) -PathType Leaf) -Message 'A journal-unresolved transaction did not preserve its pending marker.'
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
