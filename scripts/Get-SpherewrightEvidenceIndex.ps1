[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EvidenceDirectory,
    [Parameter(Mandatory)][ValidateCount(1, 16)]
    [ValidatePattern('^[0-9a-fA-F]{32}$')][string[]]$RunIds
)

# Read only the named runs' commit/terminal receipts. This is a compact index,
# never a substitute for the original receipt or the ten-write factory audit.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$accepted = @{}
$terminals = @{}
$replayResponses = 0
$rejectedCommitResponses = 0
$readRecords = 0
$unpairedTerminals = 0
$unresolvedIntents = [Collections.Generic.List[object]]::new()
$unclassifiedCommitResponses = [Collections.Generic.List[object]]::new()

foreach ($runId in $RunIds) {
    $files = @(Get-ChildItem -LiteralPath $EvidenceDirectory -File -Filter "action-$runId-*.json" |
        Where-Object { $_.Name -match '-(commit-intent|bridge-response-(commit_[A-Za-z0-9_]+|get_action_result))\.json$' } |
        Sort-Object Name)
    if ($files.Count -eq 0) { throw "No commit/action receipts found for run $runId." }
    if ($files.Count -gt 4096 -or $readRecords + $files.Count -gt 4096) {
        throw 'Explicit evidence runs exceed the bounded 4096-receipt index budget.'
    }

    $pendingIntent = $null
    foreach ($file in $files) {
        if ($file.Length -gt 2097152) { throw "An action receipt exceeds the 2 MiB index budget: $($file.Name)" }
        $record = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        if ($record.runId -cne $runId -or $null -eq $record.payload -or
            $file.Name -cne ('action-{0}-{1:D4}-{2}.json' -f $runId, [int]$record.ordinal, $record.recordType)) {
            throw "Receipt identity or response is malformed: $($file.Name)"
        }
        $readRecords++
        if ($record.recordType -ceq 'commit-intent') {
            if ($record.payload.method -notmatch '^commit_[A-Za-z0-9_]+$') { throw 'Malformed commit intent.' }
            if ($null -ne $pendingIntent) { $unresolvedIntents.Add($pendingIntent) }
            $pendingIntent = [pscustomobject]@{runId=$runId;intentOrdinal=[int]$record.ordinal;commitKind=[string]$record.payload.method}
            continue
        }
        if ($null -eq $record.payload.PSObject.Properties['response']) { throw 'Receipt has no response field.' }
        $response = $record.payload.response
        $isCommit = $record.recordType -like 'bridge-response-commit_*'
        if ($isCommit -and ($null -eq $response -or $response.success -ne $true -or $null -eq $response.result -or
            $null -eq $response.result.PSObject.Properties['accepted'] -or $response.result.accepted -isnot [bool])) {
            # Even an error envelope may be an internal/unknown failure after
            # dispatch. Do not infer zero accepted from a failed/malformed response.
            $unclassifiedCommitResponses.Add([pscustomobject]@{runId=$runId;responseOrdinal=[int]$record.ordinal;commitKind=$record.recordType.Substring('bridge-response-'.Length)})
            continue
        }
        if ($response.success -ne $true -or $null -eq $response.result) {
            if ($isCommit) { throw "Commit receipt has no trustworthy result: $($file.Name)" }
            continue
        }
        $result = $response.result
        if ($isCommit) {
            if ($null -ne $pendingIntent -and $pendingIntent.commitKind -ceq $record.recordType.Substring('bridge-response-'.Length)) { $pendingIntent = $null }
            if ($result.accepted -ne $true) { $rejectedCommitResponses++; continue }
            $actionId = [string]$result.actionId
            if ([string]::IsNullOrWhiteSpace($actionId)) { throw "Accepted commit lacks an action ID: $($file.Name)" }
            if ($result.idempotentReplay -eq $true) { $replayResponses++; continue }
            if ($accepted.ContainsKey($actionId)) { throw "Duplicate non-replay accepted action $actionId." }
            $accepted[$actionId] = [pscustomobject]@{
                actionId = $actionId
                runId = $runId
                commitOrdinal = [int]$record.ordinal
                commitKind = [string]$record.recordType.Substring('bridge-response-'.Length)
                committedAtUtc = [datetimeoffset]$record.recordedAtUtc
            }
            continue
        }
        if ($result.terminal -ne $true) { continue }
        $actionId = [string]$result.actionId
        if ([string]::IsNullOrWhiteSpace($actionId)) { throw "Terminal receipt lacks an action ID: $($file.Name)" }
        if ($terminals.ContainsKey($actionId)) {
            $prior = $terminals[$actionId]
            if ($prior.state -cne [string]$result.state -or $prior.succeeded -ne $result.succeeded) {
                throw "Conflicting terminal receipts for action $actionId."
            }
            continue
        }
        $terminals[$actionId] = [pscustomobject]@{
            state = [string]$result.state
            succeeded = ($result.succeeded -eq $true)
            terminalOrdinal = [int]$record.ordinal
            terminalAtUtc = [datetimeoffset]$record.recordedAtUtc
            completedAtGameTick = $result.completedAtGameTick
        }
    }
    if ($null -ne $pendingIntent) { $unresolvedIntents.Add($pendingIntent) }
}

$rows = @(
    foreach ($entry in $accepted.Values) {
        $terminal = $terminals[$entry.actionId]
        $wallMs = $null
        if ($null -ne $terminal) {
            $wallMs = [math]::Round(($terminal.terminalAtUtc - $entry.committedAtUtc).TotalMilliseconds, 3)
        }
        [pscustomobject]@{
            actionId = $entry.actionId
            runId = $entry.runId
            commitOrdinal = $entry.commitOrdinal
            commitKind = $entry.commitKind
            terminalOrdinal = $(if ($null -eq $terminal) { $null } else { $terminal.terminalOrdinal })
            terminalState = $(if ($null -eq $terminal) { $null } else { $terminal.state })
            succeeded = $(if ($null -eq $terminal) { $null } else { $terminal.succeeded })
            completedAtGameTick = $(if ($null -eq $terminal) { $null } else { $terminal.completedAtGameTick })
            receiptWallMs = $wallMs
        }
    }
)
$rows = @($rows | Sort-Object runId,commitOrdinal)
$unresolved = @($rows | Where-Object { $null -eq $_.terminalOrdinal } | ForEach-Object actionId)
$uncertainTerminals = @($rows | Where-Object { $_.terminalState -in @('outcome_unknown','recovery_required') } | ForEach-Object actionId)
foreach ($actionId in $terminals.Keys) {
    if (-not $accepted.ContainsKey($actionId)) { $unpairedTerminals++ }
}
[pscustomobject]@{
    readRecords = $readRecords
    acceptedUnique = $rows.Count
    replayResponses = $replayResponses
    rejectedCommitResponses = $rejectedCommitResponses
    unpairedTerminals = $unpairedTerminals
    unresolvedActionIds = $unresolved
    unresolvedCommitIntents = $unresolvedIntents.ToArray()
    unclassifiedCommitResponses = $unclassifiedCommitResponses.ToArray()
    uncertainTerminalActionIds = $uncertainTerminals
    newWriteBlocked = ($unresolved.Count -gt 0 -or $unresolvedIntents.Count -gt 0 -or $unclassifiedCommitResponses.Count -gt 0 -or $uncertainTerminals.Count -gt 0 -or $unpairedTerminals -gt 0)
    actions = $rows
    # This is not a factory snapshot, inventory comparison, or save proof.
    auditCoverage = 'action_receipts_only'
} | ConvertTo-Json -Depth 8 -Compress
