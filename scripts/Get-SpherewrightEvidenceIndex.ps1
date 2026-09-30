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

foreach ($runId in $RunIds) {
    $files = @(Get-ChildItem -LiteralPath $EvidenceDirectory -File -Filter "action-$runId-*.json" |
        Where-Object { $_.Name -match '-bridge-response-(commit_[A-Za-z0-9_]+|get_action_result)\.json$' } |
        Sort-Object Name)
    if ($files.Count -eq 0) { throw "No commit/action receipts found for run $runId." }
    if ($files.Count -gt 4096 -or $readRecords + $files.Count -gt 4096) {
        throw 'Explicit evidence runs exceed the bounded 4096-receipt index budget.'
    }

    foreach ($file in $files) {
        if ($file.Length -gt 2097152) { throw "An action receipt exceeds the 2 MiB index budget: $($file.Name)" }
        $record = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        if ($record.runId -cne $runId -or $null -eq $record.payload -or $null -eq $record.payload.response) {
            throw "Receipt identity or response is malformed: $($file.Name)"
        }
        $readRecords++
        $response = $record.payload.response
        $isCommit = $record.recordType -like 'bridge-response-commit_*'
        if ($isCommit -and $response.success -ne $true) {
            $rejectedCommitResponses++
            continue
        }
        if ($response.success -ne $true -or $null -eq $response.result) {
            if ($isCommit) { throw "Commit receipt has no trustworthy result: $($file.Name)" }
            continue
        }
        $result = $response.result
        if ($isCommit) {
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
                committedAtUtc = [string]$record.recordedAtUtc
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
            terminalAtUtc = [string]$record.recordedAtUtc
            completedAtGameTick = $result.completedAtGameTick
        }
    }
}

$rows = @(
    foreach ($entry in $accepted.Values) {
        $terminal = $terminals[$entry.actionId]
        $wallMs = $null
        if ($null -ne $terminal) {
            $wallMs = [math]::Round(([datetimeoffset]::Parse($terminal.terminalAtUtc) -
                [datetimeoffset]::Parse($entry.committedAtUtc)).TotalMilliseconds, 3)
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
    actions = $rows
    # This is not a factory snapshot, inventory comparison, or save proof.
    auditCoverage = 'action_receipts_only'
} | ConvertTo-Json -Depth 8 -Compress
