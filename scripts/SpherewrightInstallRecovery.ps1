Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'SpherewrightInstallTransaction.ps1')

function Get-SpherewrightRecoveryField {
    param([Parameter(Mandatory)][object]$Value, [Parameter(Mandatory)][string]$Name)
    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property) { throw "Recovery evidence lacks required field: $Name" }
    return ,($property.Value)
}

function Test-SpherewrightRecoveryPathExists {
    param([Parameter(Mandatory)][string]$Path)
    try { $null = [IO.File]::GetAttributes($Path); return $true }
    catch [IO.FileNotFoundException] { return $false }
    catch [IO.DirectoryNotFoundException] { return $false }
}

function Get-SpherewrightRecoveryOrdinaryFileHash {
    param([Parameter(Mandatory)][string]$Path)
    $attributes = [IO.File]::GetAttributes($Path)
    if (($attributes -band [IO.FileAttributes]::Directory) -or ($attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Recovery evidence must be an ordinary non-reparse file.' }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256 -ErrorAction Stop).Hash
}

function ConvertTo-SpherewrightRecoveryExpectedMap {
    param([Parameter(Mandatory)][object]$Entries)
    if ($Entries -isnot [Array]) { throw 'Recovery file map must be an array.' }
    $entriesArray = @($Entries)
    if ($entriesArray.Count -lt 1 -or $entriesArray.Count -gt 1024) { throw 'Recovery file map must contain 1..1024 entries.' }
    $map = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entriesArray) {
        $relative = [string](Get-SpherewrightRecoveryField $entry 'relative')
        $hash = [string](Get-SpherewrightRecoveryField $entry 'sha256')
        if ($map.ContainsKey($relative)) { throw 'Recovery file map contains duplicate paths.' }
        $map.Add($relative, $hash)
    }
    return New-SpherewrightInstallExpectedMap $map
}

function ConvertTo-SpherewrightRecoveryOriginal {
    param([Parameter(Mandatory)][object]$Snapshot, [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$Expected)
    $root = ConvertTo-SpherewrightInstallCanonicalPath ([string](Get-SpherewrightRecoveryField $Snapshot 'root'))
    if (-not [string]::Equals($root, $Target, [StringComparison]::OrdinalIgnoreCase)) { throw 'Original snapshot target does not match the explicit recovery target.' }
    $existed = Get-SpherewrightRecoveryField $Snapshot 'rootExisted'
    if ($existed -isnot [bool]) { throw 'Original root existence must be a Boolean.' }
    $entries = Get-SpherewrightRecoveryField $Snapshot 'files'
    if ($entries -isnot [Array]) { throw 'Original snapshot file evidence must be an array.' }
    if ($entries.Count -ne $Expected.Count) { throw 'Original snapshot does not cover the exact expected file set.' }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $normalized = [Collections.Generic.List[object]]::new()
    foreach ($entry in $entries) {
        $relative = [string](Get-SpherewrightRecoveryField $entry 'relative')
        $exists = Get-SpherewrightRecoveryField $entry 'exists'
        $hash = Get-SpherewrightRecoveryField $entry 'sha256'
        if (-not $Expected.ContainsKey($relative) -or -not $seen.Add($relative) -or $exists -isnot [bool]) { throw 'Original snapshot has invalid paths or existence values.' }
        if ($exists -and (-not $existed -or [string]$hash -notmatch '^[0-9a-fA-F]{64}$')) { throw 'Original snapshot contains invalid existing-file evidence.' }
        if (-not $exists -and $null -ne $hash) { throw 'An absent original file must not claim a hash.' }
        $normalized.Add([pscustomobject][ordered]@{relative=$relative;exists=$exists;sha256=if ($exists) { ([string]$hash).ToUpperInvariant() } else { $null }})
    }
    return [pscustomobject][ordered]@{root=$Target;rootExisted=$existed;files=@($normalized | Sort-Object relative)}
}

function Assert-SpherewrightRecoveryKnownLive {
    param([Parameter(Mandatory)][object]$Live, [Parameter(Mandatory)][object]$Original,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$Expected)
    foreach ($file in @($Live.files)) {
        if (-not $file.exists) { continue }
        $old = @($Original.files | Where-Object { $_.relative -ieq $file.relative })
        $isOld = $old.Count -eq 1 -and $old[0].exists -and [string]::Equals([string]$file.sha256, [string]$old[0].sha256, [StringComparison]::OrdinalIgnoreCase)
        if (-not $isOld -and -not [string]::Equals([string]$file.sha256, $Expected[$file.relative], [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unknown live payload content must not be overwritten: $($file.relative)"
        }
    }
}

function Get-SpherewrightInstallRecoveryContext {
    param([Parameter(Mandatory)][string]$PluginDestination, [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$OperationId)
    if ($OperationId -cnotmatch '^[0-9a-f]{32}$') { throw 'Recovery requires an explicit lowercase installation operation ID.' }
    $plugin = ConvertTo-SpherewrightInstallCanonicalPath $PluginDestination
    $mcp = ConvertTo-SpherewrightInstallCanonicalPath $McpDestination
    $pluginScan = Split-Path -Parent $plugin
    $pluginParent = Split-Path -Parent $pluginScan
    if ((Split-Path -Leaf $plugin) -ine 'Spherewright' -or (Split-Path -Leaf $pluginScan) -ine 'plugins' -or (Split-Path -Leaf $pluginParent) -ine 'BepInEx') {
        throw 'Recovery supports the explicit manual BepInEx/plugins/Spherewright target only.'
    }
    foreach ($target in @($plugin, $mcp)) {
        if ([string]::Equals($target, [IO.Path]::GetPathRoot($target), [StringComparison]::OrdinalIgnoreCase)) { throw 'Recovery targets cannot be filesystem roots.' }
        Assert-SpherewrightInstallNonReparseAncestors $target
    }
    if (Test-SpherewrightInstallPathOverlap $plugin $mcp) { throw 'Recovery targets overlap.' }
    if (Test-SpherewrightInstallPathOverlap $pluginScan $mcp) { throw 'MCP recovery must stay outside the Plugin scan range.' }
    $markerPath = Get-SpherewrightInstallPendingMarkerPath $pluginParent
    $markerHash = Get-SpherewrightRecoveryOrdinaryFileHash $markerPath
    $marker = Read-SpherewrightInstallPendingMarker $markerPath
    $mcpStage = ConvertTo-SpherewrightInstallCanonicalPath ([string](Get-SpherewrightRecoveryField $marker 'mcpStageRoot'))
    $mcpParent = Split-Path -Parent $mcpStage
    $mcpPrefix = $mcpParent.TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $mcp.StartsWith($mcpPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'MCP evidence parent is not an ancestor of the explicit MCP target.' }
    $identity = @{
        schemaVersion=1; operationId=$OperationId; pluginDestination=$plugin; mcpDestination=$mcp
        pluginStageRoot=(Join-Path $pluginParent ".spherewright-stage-$OperationId-plugin")
        mcpStageRoot=(Join-Path $mcpParent ".spherewright-stage-$OperationId-mcp")
        pluginArchive=(Join-Path $pluginParent ".spherewright-archive-$OperationId-plugin")
        mcpArchive=(Join-Path $mcpParent ".spherewright-archive-$OperationId-mcp")
    }
    Assert-SpherewrightInstallPendingMarkerIdentity -MarkerPath $markerPath -ExpectedIdentity $identity
    if ((Get-SpherewrightRecoveryOrdinaryFileHash $markerPath) -cne $markerHash) { throw 'Pending marker changed during recovery inspection.' }
    $roots = @{}
    foreach ($role in @('plugin','mcp')) {
        $candidates = @($identity[$role + 'StageRoot'], $identity[$role + 'Archive'])
        foreach ($candidate in $candidates) {
            Assert-SpherewrightInstallNonReparseAncestors $candidate
            foreach ($target in @($plugin,$mcp,$pluginScan)) {
                if (Test-SpherewrightInstallPathOverlap $candidate $target) { throw 'Recovery evidence overlaps a live target or Plugin scan tree.' }
            }
        }
        $present = @($candidates | Where-Object { Test-SpherewrightRecoveryPathExists $_ })
        if ($present.Count -ne 1) { throw "Recovery requires exactly one stage/archive for $role; no guessing is allowed." }
        $roots[$role] = $present[0]
    }
    if (Test-SpherewrightInstallPathOverlap $roots.plugin $roots.mcp) { throw 'Recovery evidence roots overlap.' }
    $records = @{}
    $recordPaths = @{}
    $recordHashes = @{}
    $immutable = @{}
    $expected = @{}
    $original = @{}
    foreach ($role in @('plugin','mcp')) {
        $transactionRoot = Join-Path $roots[$role] 'transaction'
        Assert-SpherewrightInstallNonReparseAncestors $transactionRoot
        $recordPath = Join-Path $transactionRoot 'progress.json'
        $recordHashes[$role] = Get-SpherewrightRecoveryOrdinaryFileHash $recordPath
        $record = Read-SpherewrightInstallBoundedProgressRecord $recordPath
        if ((Get-SpherewrightRecoveryOrdinaryFileHash $recordPath) -cne $recordHashes[$role]) { throw 'Recovery progress changed while being read.' }
        if ([int](Get-SpherewrightRecoveryField $record 'schemaVersion') -ne 1 -or [string](Get-SpherewrightRecoveryField $record 'operationId') -cne $OperationId) { throw 'Recovery record identity is invalid.' }
        foreach ($name in @('pluginDestination','mcpDestination','pluginArchive','mcpArchive')) {
            $path = ConvertTo-SpherewrightInstallCanonicalPath ([string](Get-SpherewrightRecoveryField $record $name))
            if (-not [string]::Equals($path, $identity[$name], [StringComparison]::OrdinalIgnoreCase)) { throw "Recovery record disagrees about $name." }
        }
        $recordMarker = ConvertTo-SpherewrightInstallCanonicalPath ([string](Get-SpherewrightRecoveryField $record 'pendingMarkerPath'))
        if (-not [string]::Equals($recordMarker, $markerPath, [StringComparison]::OrdinalIgnoreCase)) { throw 'Recovery record marker path disagrees.' }
        $status = [string](Get-SpherewrightRecoveryField $record 'status')
        if ($status -notin @('pending','failed_before_live','finalizing','committed','rolling_back','rolled_back','needs_recovery')) { throw 'Recovery record has an unknown status.' }
        $pMap = ConvertTo-SpherewrightRecoveryExpectedMap (Get-SpherewrightRecoveryField $record 'pluginExpectedFiles')
        $mMap = ConvertTo-SpherewrightRecoveryExpectedMap (Get-SpherewrightRecoveryField $record 'mcpExpectedFiles')
        $names = @('Spherewright.Plugin.dll','Spherewright.Contracts.dll','Spherewright.Bridge.Core.dll','Newtonsoft.Json.dll')
        if ($pMap.Count -ne 4 -or @($names | Where-Object { -not $pMap.ContainsKey($_) }).Count -gt 0 -or -not $mMap.ContainsKey('Spherewright.Mcp.exe')) { throw 'Recovery record does not describe supported Plugin/MCP payloads.' }
        $pOriginal = ConvertTo-SpherewrightRecoveryOriginal (Get-SpherewrightRecoveryField $record 'pluginOriginal') $plugin $pMap
        $mOriginal = ConvertTo-SpherewrightRecoveryOriginal (Get-SpherewrightRecoveryField $record 'mcpOriginal') $mcp $mMap
        $handoff = [string](Get-SpherewrightRecoveryField $record 'runtimeHandoffFingerprint')
        $projection = [ordered]@{
            pluginOriginal=$pOriginal; mcpOriginal=$mOriginal; handoff=$handoff
            pluginExpected=@($pMap.Keys | Sort-Object | ForEach-Object { "$_=$($pMap[$_].ToUpperInvariant())" })
            mcpExpected=@($mMap.Keys | Sort-Object | ForEach-Object { "$_=$($mMap[$_].ToUpperInvariant())" })
        }
        $immutable[$role] = $projection | ConvertTo-Json -Depth 12 -Compress
        $records[$role] = $record
        $recordPaths[$role] = $recordPath
        if ($role -eq 'plugin') { $expected=@{plugin=$pMap;mcp=$mMap}; $original=@{plugin=$pOriginal;mcp=$mOriginal} }
    }
    if ($immutable.plugin -cne $immutable.mcp) { throw 'Mirrored recovery records disagree on immutable payload evidence.' }
    $handoffFingerprint = [string]$records.plugin.runtimeHandoffFingerprint
    if ((Get-SpherewrightInstallTreeFingerprint (Join-Path $plugin 'runtime-handoff')) -cne $handoffFingerprint) { throw 'Protected runtime-handoff changed; recovery refused.' }
    $live = @{}
    $backups = @{}
    $prior = @{}
    foreach ($role in @('plugin','mcp')) {
        $backup = Join-Path $roots[$role] 'transaction/old-payload'
        Assert-SpherewrightInstallNonReparseAncestors $backup
        $backupExpected = New-SpherewrightInstallBackupMap $original[$role]
        $backups[$role] = Assert-SpherewrightInstallSnapshotComplete -Root $backup -ExpectedFiles $backupExpected
        $target = $identity[$role + 'Destination']
        $preserved = if ($role -eq 'plugin') { @('runtime-handoff') } else { @() }
        $live[$role] = Get-SpherewrightInstallPayloadSnapshot -Root $target -ExpectedFiles $expected[$role] -PreservedDirectories $preserved
        Assert-SpherewrightRecoveryKnownLive $live[$role] $original[$role] $expected[$role]
    }
    $priorMain = Join-Path $roots.plugin 'transaction/prior-main.dll'
    $prior.plugin = $null
    if (Test-SpherewrightRecoveryPathExists $priorMain) {
        $item = Get-Item -LiteralPath $priorMain -Force -ErrorAction Stop
        if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Prior main evidence is not an ordinary file.' }
        $prior.plugin = (Get-FileHash -LiteralPath $priorMain -Algorithm SHA256 -ErrorAction Stop).Hash
        $oldMain = @($original.plugin.files | Where-Object relative -ieq 'Spherewright.Plugin.dll')[0]
        if (-not $oldMain.exists -or $prior.plugin -ine $oldMain.sha256) { throw 'Prior main evidence does not match the original.' }
    }
    $priorMcp = Join-Path $roots.mcp 'transaction/prior-live'
    $prior.mcp = $null
    if (Test-SpherewrightRecoveryPathExists $priorMcp) {
        Assert-SpherewrightInstallNonReparseAncestors $priorMcp
        if (-not (Test-SpherewrightInstallSnapshotMatches $original.mcp $priorMcp $expected.mcp)) { throw 'Prior MCP evidence does not match the original.' }
        $prior.mcp = Get-SpherewrightInstallPayloadSnapshot $priorMcp $expected.mcp
    }
    $evidence = [ordered]@{
        schema=1; operationId=$OperationId; plugin=$plugin; mcp=$mcp
        pluginEvidenceRoot=$roots.plugin; mcpEvidenceRoot=$roots.mcp
        markerHash=$markerHash; pluginRecordHash=$recordHashes.plugin; mcpRecordHash=$recordHashes.mcp
        livePlugin=$live.plugin; liveMcp=$live.mcp; backupPlugin=$backups.plugin; backupMcp=$backups.mcp
        priorPlugin=$prior.plugin; priorMcp=$prior.mcp; handoff=$handoffFingerprint
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($evidence | ConvertTo-Json -Depth 16 -Compress))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $digest = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','').ToLowerInvariant() }
    finally { $sha.Dispose() }
    if ((Get-SpherewrightRecoveryOrdinaryFileHash $markerPath) -cne $markerHash) { throw 'Pending marker changed during recovery inspection.' }
    foreach ($role in @('plugin','mcp')) {
        if ((Get-SpherewrightRecoveryOrdinaryFileHash $recordPaths[$role]) -cne $recordHashes[$role]) { throw 'Recovery progress changed during inspection.' }
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $handoffDigest = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($handoffFingerprint))).Replace('-','').ToLowerInvariant() }
    finally { $sha.Dispose() }
    $previewEvidence = [ordered]@{
        markerSha256=$markerHash
        pluginProgress=[ordered]@{sha256=$recordHashes.plugin;root=$roots.plugin;status=$records.plugin.status;phase=$records.plugin.phase}
        mcpProgress=[ordered]@{sha256=$recordHashes.mcp;root=$roots.mcp;status=$records.mcp.status;phase=$records.mcp.phase}
        pluginBackup=$backups.plugin; mcpBackup=$backups.mcp
        originalPlugin=$original.plugin; originalMcp=$original.mcp
        currentPlugin=$live.plugin; currentMcp=$live.mcp
        priorMainSha256=$prior.plugin; priorMcp=$prior.mcp
        runtimeHandoffSha256=$handoffDigest
    }
    return @{identity=$identity; markerPath=$markerPath; roots=$roots; record=$records.plugin; expected=$expected; original=$original; handoff=$handoffFingerprint; evidenceHash=$digest; live=$live; previewEvidence=$previewEvidence}
}

function Get-SpherewrightInstallRecoveryPreview {
    param([Parameter(Mandatory)][string]$PluginDestination, [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$OperationId)
    $targets = @((ConvertTo-SpherewrightInstallCanonicalPath $PluginDestination), (ConvertTo-SpherewrightInstallCanonicalPath $McpDestination))
    $locks = Enter-SpherewrightInstallTargetLocks -Targets $targets -AllowAbandonedForExplicitRecovery
    try {
        if (Get-Process -Name DSPGAME -ErrorAction SilentlyContinue) { throw 'Close DSPGAME before inspecting an installation for recovery.' }
        $context = Get-SpherewrightInstallRecoveryContext @PSBoundParameters
        return [pscustomobject][ordered]@{mode='preview'; operationId=$OperationId; evidenceHash=$context.evidenceHash; evidence=$context.previewEvidence; restoreAllowed=$true; writesPerformed=$false; automaticRecovery=$false}
    } finally { Exit-SpherewrightInstallTargetLocks $locks }
}

function Invoke-SpherewrightInstallRecovery {
    param([Parameter(Mandatory)][string]$PluginDestination, [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string]$OperationId, [Parameter(Mandatory)][string]$ExpectedEvidenceHash,
        [scriptblock]$BeforeMutation)
    if ($ExpectedEvidenceHash -notmatch '^[0-9a-fA-F]{64}$') { throw 'Explicit recovery requires a fresh preview evidence hash.' }
    $targets = @((ConvertTo-SpherewrightInstallCanonicalPath $PluginDestination), (ConvertTo-SpherewrightInstallCanonicalPath $McpDestination))
    $locks = Enter-SpherewrightInstallTargetLocks -Targets $targets -AllowAbandonedForExplicitRecovery
    $state = $null
    try {
        if (Get-Process -Name DSPGAME -ErrorAction SilentlyContinue) { throw 'Close DSPGAME before installation recovery.' }
        $argsForRead = @{PluginDestination=$targets[0]; McpDestination=$targets[1]; OperationId=$OperationId}
        $context = Get-SpherewrightInstallRecoveryContext @argsForRead
        if ($context.evidenceHash -ine $ExpectedEvidenceHash) { throw 'Recovery evidence changed; obtain a fresh preview. No recovery write was performed.' }
        Invoke-SpherewrightInstallFaultHook $BeforeMutation 'recovery-before-revalidation'
        $context = Get-SpherewrightInstallRecoveryContext @argsForRead
        if ($context.evidenceHash -ine $ExpectedEvidenceHash) { throw 'Recovery evidence changed immediately before execution. No recovery write was performed.' }
        if (Get-Process -Name DSPGAME -ErrorAction SilentlyContinue) { throw 'DSPGAME started before recovery; no recovery write was performed.' }
        $attemptId = [guid]::NewGuid().ToString('N')
        $record = @{}
        foreach ($property in $context.record.PSObject.Properties) { $record[$property.Name] = $property.Value }
        $record['recoveryAttemptId'] = $attemptId
        $record['status'] = 'rolling_back'
        $record['recoveryEvidenceHash'] = $ExpectedEvidenceHash.ToLowerInvariant()
        $state = @{
            operationId=$OperationId; beforeMutation=$BeforeMutation; record=$record
            pluginDestination=$targets[0]; mcpDestination=$targets[1]
            pluginStageRoot=$context.identity.pluginStageRoot; mcpStageRoot=$context.identity.mcpStageRoot
            pluginStageParent=(Split-Path -Parent $context.identity.pluginStageRoot)
            mcpStageParent=(Split-Path -Parent $context.identity.mcpStageRoot)
            pluginArchive=$context.identity.pluginArchive; mcpArchive=$context.identity.mcpArchive
            pluginDataRoot=$context.roots.plugin; mcpDataRoot=$context.roots.mcp
            pluginTransactionRoot=(Join-Path $context.roots.plugin 'transaction')
            mcpTransactionRoot=(Join-Path $context.roots.mcp 'transaction')
            pendingMarkerPath=$context.markerPath; pendingMarkerIdentity=$context.identity; pendingMarkerCreated=$true
            rollbackMode=$false; rollbackJournalErrors=[Collections.Generic.List[string]]::new(); unresolvedMainQuiesced=$false
        }
        $state.pluginBackupRoot = Join-Path $state.pluginTransactionRoot 'old-payload'
        $state.mcpBackupRoot = Join-Path $state.mcpTransactionRoot 'old-payload'
        $state.pluginPriorMain = Join-Path $state.pluginTransactionRoot 'prior-main.dll'
        $state.mcpPriorLiveRoot = Join-Path $state.mcpTransactionRoot 'prior-live'
        # Keep evidence paths short for the supported .NET Framework shell.
        # The full random attempt identity is retained; directories are exclusive.
        $state.pluginFailureRoot = Join-Path $state.pluginTransactionRoot "r-$attemptId/p"
        $state.mcpFailureRoot = Join-Path $state.mcpTransactionRoot "r-$attemptId/m"
        foreach ($evidenceRoot in @($state.pluginFailureRoot, $state.mcpFailureRoot)) {
            if (Test-SpherewrightRecoveryPathExists $evidenceRoot) { throw 'Recovery attempt evidence already exists; it must not be overwritten.' }
        }
        Set-SpherewrightInstallJournalPaths $state
        Write-SpherewrightInstallProgress -State $state -Phase 'recovery-prepared'
        foreach ($role in @('plugin','mcp')) {
            $attemptRoot = Split-Path -Parent $state[$role + 'FailureRoot']
            if (Test-SpherewrightRecoveryPathExists $attemptRoot) { throw 'Recovery attempt directory already exists; it must not be reused.' }
            Invoke-SpherewrightInstallMutation -State $state -Step ('create-recovery-' + $role + '-evidence') -Action {
                [void][IO.Directory]::CreateDirectory($attemptRoot)
            }
        }
        # This explicitly authorized attempt withholds ANY live main, even an
        # old binary which predates the startup guard. Never remove its backup.
        Quiesce-SpherewrightInstallAnyLiveMainForUnresolvedFailure $state
        if ($state.rollbackJournalErrors.Count -gt 0) { throw 'Recovery could not durably record main withholding.' }
        Restore-SpherewrightInstallOriginalPayloads -State $state -PluginOriginal $context.original.plugin -McpOriginal $context.original.mcp -PluginExpectedFiles $context.expected.plugin -McpExpectedFiles $context.expected.mcp -HandoffFingerprint $context.handoff
        $state.record['status'] = 'finalizing'
        Write-SpherewrightInstallProgress -State $state -Phase 'finalizing-explicit-recovery'
        Complete-SpherewrightInstallTerminalArchive $state
        $state.record['status'] = 'rolled_back'
        Write-SpherewrightInstallProgress -State $state -Phase 'recovered-original'
        Complete-SpherewrightInstallPendingMarker -State $state -Outcome rolled_back -PluginExpectedFiles $context.expected.plugin -McpExpectedFiles $context.expected.mcp -PluginOriginal $context.original.plugin -McpOriginal $context.original.mcp -HandoffFingerprint $context.handoff
        return [pscustomobject][ordered]@{status='rolled_back'; restoredOriginal=$true; operationId=$OperationId; recoveryAttemptId=$attemptId; automaticRecovery=$false; legacyStartupRaceClosed=$false}
    } catch {
        $failure = $_
        if ($null -eq $state) { throw }
        $followup = [Collections.Generic.List[string]]::new()
        $state.record['status'] = 'needs_recovery'
        try { Write-SpherewrightInstallProgress -State $state -Phase 'explicit-recovery-failed' }
        catch { $followup.Add($_.Exception.Message) }
        # Archiving can move the operation roots. Derive the failure path from
        # their current location and retain a separate copy of any restored main.
        $state.pluginFailureRoot = Join-Path $state.pluginTransactionRoot "r-$attemptId/f"
        try { Quiesce-SpherewrightInstallAnyLiveMainForUnresolvedFailure $state }
        catch { $followup.Add('Live main could not be withheld: ' + $_.Exception.Message) }
        $details = @($followup) -join ' | '
        throw [InvalidOperationException]::new("Explicit recovery did not complete; preserve marker and evidence. $($failure.Exception.Message) $details", $failure.Exception)
    } finally { Exit-SpherewrightInstallTargetLocks $locks }
}
