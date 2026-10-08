# Offline exact-file fixture for the protected evidence reader; no Bridge calls.
[CmdletBinding()]
param([switch]$Benchmark)

$ErrorActionPreference = 'Stop'
$script:readerBridgeCalls = 0
$script:readerChecks = 0
$script:benchmarkLegacyReadCount = 0
$script:fixtureDirectory = $null
$script:fixtureFiles = [Collections.Generic.List[string]]::new()
$script:readerEncoding = [Text.UTF8Encoding]::new($false, $true)
$benchmarkSummary = $null

function Invoke-SpherewrightBridgeRequest {
    $script:readerBridgeCalls++
    throw 'The evidence reader fixture must not call the Bridge.'
}

. (Join-Path $PSScriptRoot 'SpherewrightFactoryEvidence.ps1')
if ($script:readerBridgeCalls -ne 0) { throw 'Importing the evidence reader issued a Bridge call.' }

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$script:fixtureDirectory = Join-Path $tempRoot ('spherewright-evidence-reader-' + [guid]::NewGuid().ToString('N'))
$fixtureFullPath = [IO.Path]::GetFullPath($script:fixtureDirectory)
if (-not $fixtureFullPath.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The evidence-reader fixture path escaped the temporary directory.'
}
$null = New-Item -ItemType Directory -Path $script:fixtureDirectory
$script:runId = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$script:otherRunId = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'

function Add-ReaderFile([string]$Name, [byte[]]$Bytes) {
    $path = Join-Path $script:fixtureDirectory $Name
    [IO.File]::WriteAllBytes($path, $Bytes)
    $null = $script:fixtureFiles.Add($path)
    return $path
}

function New-ReaderRecord([string]$RunId, [int]$Ordinal, [string]$RecordType) {
    [pscustomobject]@{
        runId = $RunId
        ordinal = $Ordinal
        recordType = $RecordType
        payload = @{ response = @{ success = $true; result = @{ accepted = $true; actionId = 'reader-action' } } }
    }
}

function Write-ReaderRecord([string]$Name, $Record) {
    $json = ConvertTo-Json -InputObject $Record -Depth 10 -Compress
    Add-ReaderFile $Name $script:readerEncoding.GetBytes($json)
}

function Read-BenchmarkDirectoryTarget([string]$Directory, [string]$TargetName) {
    $records = @(
        foreach ($file in Get-ChildItem -LiteralPath $Directory -File -Filter '*.json') {
            [pscustomobject]@{
                name = $file.Name
                record = ([IO.File]::ReadAllText($file.FullName, $script:readerEncoding) | ConvertFrom-Json)
            }
        }
    )
    $script:benchmarkLegacyReadCount = $records.Count
    $matches = @($records | Where-Object { $_.name -ceq $TargetName })
    if ($matches.Count -ne 1) { throw 'The directory-scan benchmark did not find exactly one target record.' }
    return $matches[0].record
}

function Assert-Reader([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Evidence reader regression: $Name" }
    $script:readerChecks++
}

function Assert-ReaderThrows([scriptblock]$Action, [string]$Name) {
    $caught = $false
    try { & $Action | Out-Null } catch { $caught = $true }
    Assert-Reader $caught $Name
}

try {
    $mainName = "action-$script:runId-0001-bridge-response-commit_build.json"
    $mainPath = Write-ReaderRecord $mainName (New-ReaderRecord $script:runId 1 'bridge-response-commit_build')
    # A same-run, standard-name but malformed decoy catches directory/run-wide parsing.
    $decoyName = "action-$script:runId-9999-bridge-response-commit_save.json"
    Add-ReaderFile $decoyName $script:readerEncoding.GetBytes('{malformed decoy') | Out-Null

    $record = Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $mainName
    Assert-Reader ($record.runId -ceq $script:runId -and $record.ordinal -eq 1 -and
        $record.recordType -ceq 'bridge-response-commit_build' -and $record.payload.response.result.actionId -ceq 'reader-action') `
        'reads and returns one explicitly named original record despite a malformed decoy'

    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $validHash = ([BitConverter]::ToString($hasher.ComputeHash([IO.File]::ReadAllBytes($mainPath))).Replace('-', '')) }
    finally { $hasher.Dispose() }
    $hashedRecord = Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $mainName -ExpectedSha256 $validHash.ToUpperInvariant()
    Assert-Reader ($hashedRecord.ordinal -eq 1) 'accepts a matching SHA-256 over the exact file bytes'
    $wrongHash = if ($validHash.StartsWith('0')) { '1' + $validHash.Substring(1) } else { '0' + $validHash.Substring(1) }
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $mainName -ExpectedSha256 $wrongHash } 'rejects a mismatched SHA-256'

    $invalidName = "action-$script:runId-001-bridge-response-commit_build.json"
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $invalidName } 'rejects a record name outside the exact filename grammar'
    $missingName = "action-$script:runId-0002-bridge-response-commit_save.json"
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $missingName } 'rejects a missing named record'

    $badJsonName = "action-$script:runId-0003-bridge-response-commit_save.json"
    Add-ReaderFile $badJsonName $script:readerEncoding.GetBytes('{not-json') | Out-Null
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $badJsonName } 'rejects malformed JSON'

    $wrongIdentityName = "action-$script:runId-0004-bridge-response-commit_save.json"
    Write-ReaderRecord $wrongIdentityName (New-ReaderRecord $script:otherRunId 4 'bridge-response-commit_save') | Out-Null
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $wrongIdentityName } 'rejects record identity that disagrees with the filename'

    $wrongOrdinalName = "action-$script:runId-0008-bridge-response-commit_save.json"
    $wrongOrdinalRecord = New-ReaderRecord $script:runId 9 'bridge-response-commit_save'
    Write-ReaderRecord $wrongOrdinalName $wrongOrdinalRecord | Out-Null
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $wrongOrdinalName } 'rejects an ordinal that disagrees with the filename'

    $wrongTypeName = "action-$script:runId-0009-bridge-response-commit_build.json"
    $wrongTypeRecord = New-ReaderRecord $script:runId 9 'bridge-response-commit_save'
    Write-ReaderRecord $wrongTypeName $wrongTypeRecord | Out-Null
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $wrongTypeName } 'rejects a record type that disagrees with the filename'

    $missingPayloadName = "action-$script:runId-0010-bridge-response-commit_build.json"
    Write-ReaderRecord $missingPayloadName ([pscustomobject]@{runId=$script:runId;ordinal=10;recordType='bridge-response-commit_build'}) | Out-Null
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $missingPayloadName } 'rejects a record with no payload'

    $limit = 8 * 1024 * 1024
    $exactLimitName = "action-$script:runId-0005-bridge-response-commit_save.json"
    $exactJson = ConvertTo-Json -InputObject (New-ReaderRecord $script:runId 5 'bridge-response-commit_save') -Depth 10 -Compress
    $exactBytes = $script:readerEncoding.GetBytes($exactJson + (' ' * ($limit - $script:readerEncoding.GetByteCount($exactJson))))
    if ($exactBytes.Length -ne $limit) { throw 'The exact-size evidence fixture has the wrong byte length.' }
    $exactPath = Add-ReaderFile $exactLimitName $exactBytes
    $exactRecord = Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $exactLimitName
    Assert-Reader ($exactRecord.ordinal -eq 5 -and (Get-Item -LiteralPath $exactPath).Length -eq $limit) 'accepts a record exactly at the 8 MiB limit'

    $oversizeName = "action-$script:runId-0006-bridge-response-commit_save.json"
    $oversizePath = Join-Path $script:fixtureDirectory $oversizeName
    $stream = [IO.File]::Open($oversizePath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.SetLength($limit + 1) } finally { $stream.Dispose() }
    $null = $script:fixtureFiles.Add($oversizePath)
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $oversizeName } 'rejects a record one byte over the 8 MiB limit'

    $reparseTarget = Join-Path $script:fixtureDirectory 'reader-target-directory'
    $null = New-Item -ItemType Directory -Path $reparseTarget
    $null = $script:fixtureFiles.Add($reparseTarget)
    $reparseName = "action-$script:runId-0007-bridge-response-commit_save.json"
    $reparsePath = Join-Path $script:fixtureDirectory $reparseName
    $null = New-Item -ItemType Junction -Path $reparsePath -Target $reparseTarget -ErrorAction Stop
    $null = $script:fixtureFiles.Add($reparsePath)
    $reparseItem = Get-Item -LiteralPath $reparsePath
    Assert-Reader (($reparseItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) 'fixture is an actual filesystem reparse point'
    Assert-ReaderThrows { Read-SpherewrightEvidenceRecord -EvidenceDirectory $script:fixtureDirectory -RecordName $reparseName } 'rejects a reparse-point evidence record'

    if ($Benchmark) {
        $benchmarkDirectory = Join-Path $script:fixtureDirectory 'benchmark-only'
        $null = New-Item -ItemType Directory -Path $benchmarkDirectory
        $null = $script:fixtureFiles.Add($benchmarkDirectory)
        $benchmarkRunId = 'cccccccccccccccccccccccccccccccc'
        $benchmarkType = 'bridge-response-commit_build'
        $targetName = $null
        for ($ordinal = 1; $ordinal -le 200; $ordinal++) {
            $name = 'action-{0}-{1:D4}-{2}.json' -f $benchmarkRunId,$ordinal,$benchmarkType
            if ($ordinal -eq 100) { $targetName = $name }
            $record = New-ReaderRecord $benchmarkRunId $ordinal $benchmarkType
            $record.payload.response.result.actionId = "benchmark-action-$ordinal"
            $json = ConvertTo-Json -InputObject $record -Depth 10 -Compress
            $path = Join-Path $benchmarkDirectory $name
            [IO.File]::WriteAllBytes($path, $script:readerEncoding.GetBytes($json))
            $null = $script:fixtureFiles.Add($path)
        }

        $warmLegacy = Read-BenchmarkDirectoryTarget $benchmarkDirectory $targetName
        $warmExact = Read-SpherewrightEvidenceRecord -EvidenceDirectory $benchmarkDirectory -RecordName $targetName
        $expectedJson = ConvertTo-Json -InputObject $warmExact -Depth 10 -Compress
        $benchmarkConsistent = ((ConvertTo-Json -InputObject $warmLegacy -Depth 10 -Compress) -ceq $expectedJson)
        $legacyTimes = [Collections.Generic.List[double]]::new()
        $exactTimes = [Collections.Generic.List[double]]::new()
        for ($iteration = 0; $iteration -lt 3; $iteration++) {
            $timer = [Diagnostics.Stopwatch]::StartNew()
            $legacyResult = Read-BenchmarkDirectoryTarget $benchmarkDirectory $targetName
            $timer.Stop(); $legacyTimes.Add($timer.Elapsed.TotalMilliseconds)
            $benchmarkConsistent = $benchmarkConsistent -and
                ((ConvertTo-Json -InputObject $legacyResult -Depth 10 -Compress) -ceq $expectedJson) -and
                $script:benchmarkLegacyReadCount -eq 200

            $timer = [Diagnostics.Stopwatch]::StartNew()
            $exactResult = Read-SpherewrightEvidenceRecord -EvidenceDirectory $benchmarkDirectory -RecordName $targetName
            $timer.Stop(); $exactTimes.Add($timer.Elapsed.TotalMilliseconds)
            $benchmarkConsistent = $benchmarkConsistent -and
                ((ConvertTo-Json -InputObject $exactResult -Depth 10 -Compress) -ceq $expectedJson)
        }
        Assert-Reader ($benchmarkConsistent) 'warmup and measured directory/exact-reader results agree on the same target'
        $legacySorted = @($legacyTimes.ToArray() | Sort-Object)
        $exactSorted = @($exactTimes.ToArray() | Sort-Object)
        $benchmarkSummary = [pscustomobject]@{
            fixtureRecords=200
            readRecords='200->1'
            warmups=1
            measuredRuns=3
            medianMs=[pscustomobject]@{directoryScan=[math]::Round($legacySorted[1],3);exactReader=[math]::Round($exactSorted[1],3)}
        }
    }

    Assert-Reader ($script:readerBridgeCalls -eq 0) 'all import and reader fixtures make zero Bridge calls'
    [pscustomobject]@{passed=$script:readerChecks;bridgeCalls=$script:readerBridgeCalls;directory=$null;benchmark=$benchmarkSummary} | ConvertTo-Json -Compress
} finally {
    for ($index = $script:fixtureFiles.Count - 1; $index -ge 0; $index--) {
        $path = $script:fixtureFiles[$index]
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
    if ($script:fixtureDirectory -and (Test-Path -LiteralPath $script:fixtureDirectory)) {
        Remove-Item -LiteralPath $script:fixtureDirectory -Force
    }
}
