Set-StrictMode -Version Latest

$script:SpherewrightInstallTransactionMainPluginFile = 'Spherewright.Plugin.dll'

function ConvertTo-SpherewrightInstallCanonicalPath {
    param([Parameter(Mandatory)][string]$Path)

    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($full)
    if ([string]::IsNullOrWhiteSpace($root)) { throw "Path has no filesystem root: $Path" }
    if (-not [string]::Equals($full, $root, [StringComparison]::OrdinalIgnoreCase)) {
        $full = $full.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    }
    return $full
}

function Test-SpherewrightInstallPathOverlap {
    param(
        [Parameter(Mandatory)][string]$First,
        [Parameter(Mandatory)][string]$Second
    )

    $firstFull = ConvertTo-SpherewrightInstallCanonicalPath $First
    $secondFull = ConvertTo-SpherewrightInstallCanonicalPath $Second
    if ([string]::Equals($firstFull, $secondFull, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    $firstPrefix = $firstFull + [IO.Path]::DirectorySeparatorChar
    $secondPrefix = $secondFull + [IO.Path]::DirectorySeparatorChar
    return $firstPrefix.StartsWith($secondPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        $secondPrefix.StartsWith($firstPrefix, [StringComparison]::OrdinalIgnoreCase)
}

function New-SpherewrightInstallExpectedMap {
    param([Parameter(Mandatory)][object]$ExpectedFiles)

    if ($null -eq $ExpectedFiles -or $null -eq $ExpectedFiles.Keys) {
        throw 'Expected files must be a keyed path-to-SHA256 map.'
    }
    $result = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($rawKey in @($ExpectedFiles.Keys)) {
        $relative = [string]$rawKey
        $hash = [string]$ExpectedFiles[$rawKey]
        if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or
            $relative.Contains('\') -or $relative.Contains(':') -or
            @($relative.Split('/') | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0) {
            throw "Expected file path is unsafe: $relative"
        }
        if ($hash -notmatch '^[0-9a-fA-F]{64}$') { throw "Expected SHA-256 is invalid: $relative" }
        # Dictionary.TryAdd is not available on the .NET Framework used by
        # Windows PowerShell 5.1, which remains a supported installer host.
        if ($result.ContainsKey($relative)) { throw "Expected file path is duplicated: $relative" }
        $result.Add($relative, $hash)
    }
    if ($result.Count -eq 0) { throw 'Expected files must not be empty.' }
    return $result
}

function Test-SpherewrightInstallExpectedDirectory {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles
    )

    $prefix = $RelativePath + '/'
    foreach ($expected in $ExpectedFiles.Keys) {
        if ($expected.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

function Assert-SpherewrightInstallNoReparsePointBelow {
    param([Parameter(Mandatory)][string]$Root)

    $rootItem = Get-Item -LiteralPath $Root -Force -ErrorAction Stop
    if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $rootItem.PSIsContainer) {
        throw "Reparse point or non-directory is forbidden: $Root"
    }
    $directories = [Collections.Generic.Stack[string]]::new()
    $directories.Push($rootItem.FullName)
    while ($directories.Count -gt 0) {
        $directory = $directories.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse point is forbidden below: $Root" }
            if ($item.PSIsContainer) { $directories.Push($item.FullName) }
        }
    }
}

function Get-SpherewrightInstallPayloadSnapshot {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles,
        [string[]]$PreservedDirectories = @()
    )

    $resolvedRoot = ConvertTo-SpherewrightInstallCanonicalPath $Root
    $rootItem = Get-Item -LiteralPath $resolvedRoot -Force -ErrorAction SilentlyContinue
    $observed = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    $rootExisted = $null -ne $rootItem
    if ($rootExisted) {
        if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $rootItem.PSIsContainer) {
            throw "Payload root must be a non-reparse directory: $resolvedRoot"
        }
        $directories = [Collections.Generic.Stack[object]]::new()
        $directories.Push([pscustomobject]@{ path=$rootItem.FullName; relative='' })
        while ($directories.Count -gt 0) {
            $current = $directories.Pop()
            foreach ($item in @(Get-ChildItem -LiteralPath $current.path -Force -ErrorAction Stop)) {
                if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse point is forbidden below: $resolvedRoot" }
                $relative = if ([string]::IsNullOrEmpty([string]$current.relative)) { $item.Name } else { "$($current.relative)/$($item.Name)" }
                if ($item.PSIsContainer) {
                    if ($PreservedDirectories -contains $relative) {
                        Assert-SpherewrightInstallNoReparsePointBelow -Root $item.FullName
                        continue
                    }
                    if (-not (Test-SpherewrightInstallExpectedDirectory -RelativePath $relative -ExpectedFiles $ExpectedFiles)) {
                        throw "Payload contains an unapproved directory: $relative"
                    }
                    $directories.Push([pscustomobject]@{ path=$item.FullName; relative=$relative })
                    continue
                }
                if (-not $ExpectedFiles.ContainsKey($relative)) { throw "Payload contains an unapproved file: $relative" }
                $observed.Add($relative, (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256 -ErrorAction Stop).Hash)
            }
        }
    }

    $files = [Collections.Generic.List[object]]::new()
    foreach ($relative in @($ExpectedFiles.Keys | Sort-Object)) {
        $exists = $observed.ContainsKey($relative)
        $files.Add([pscustomobject][ordered]@{
            relative = $relative
            exists = $exists
            sha256 = if ($exists) { $observed[$relative] } else { $null }
        })
    }
    return [pscustomobject][ordered]@{
        root = $resolvedRoot
        rootExisted = $rootExisted
        files = @($files)
    }
}

function Assert-SpherewrightInstallSnapshotComplete {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles,
        [string[]]$PreservedDirectories = @()
    )

    $snapshot = Get-SpherewrightInstallPayloadSnapshot -Root $Root -ExpectedFiles $ExpectedFiles -PreservedDirectories $PreservedDirectories
    if (-not $snapshot.rootExisted) { throw "Payload root is missing: $Root" }
    foreach ($file in @($snapshot.files)) {
        if (-not $file.exists) { throw "Payload file is missing: $($file.relative)" }
        if (-not [string]::Equals([string]$file.sha256, $ExpectedFiles[$file.relative], [StringComparison]::OrdinalIgnoreCase)) {
            throw "Payload integrity verification failed: $($file.relative)"
        }
    }
    return $snapshot
}

function Test-SpherewrightInstallSnapshotMatches {
    param(
        [Parameter(Mandatory)][object]$ExpectedSnapshot,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles,
        [string[]]$PreservedDirectories = @()
    )

    $actual = Get-SpherewrightInstallPayloadSnapshot -Root $Root -ExpectedFiles $ExpectedFiles -PreservedDirectories $PreservedDirectories
    if ([bool]$actual.rootExisted -ne [bool]$ExpectedSnapshot.rootExisted) { return $false }
    foreach ($expectedFile in @($ExpectedSnapshot.files)) {
        $actualFile = @($actual.files | Where-Object { $_.relative -ceq $expectedFile.relative })
        if ($actualFile.Count -ne 1 -or [bool]$actualFile[0].exists -ne [bool]$expectedFile.exists) { return $false }
        if ($expectedFile.exists -and -not [string]::Equals([string]$actualFile[0].sha256, [string]$expectedFile.sha256, [StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
    }
    return $true
}

function Get-SpherewrightInstallTreeFingerprint {
    param([Parameter(Mandatory)][string]$Root)

    $item = Get-Item -LiteralPath $Root -Force -ErrorAction SilentlyContinue
    if ($null -eq $item) { return '<missing>' }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $item.PSIsContainer) {
        throw "Protected tree must be a non-reparse directory: $Root"
    }
    $entries = [Collections.Generic.List[string]]::new()
    $entries.Add('D|')
    $directories = [Collections.Generic.Stack[object]]::new()
    $directories.Push([pscustomobject]@{ path=$item.FullName; relative='' })
    while ($directories.Count -gt 0) {
        $current = $directories.Pop()
        foreach ($child in @(Get-ChildItem -LiteralPath $current.path -Force -ErrorAction Stop)) {
            if ($child.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse point is forbidden below protected tree: $Root" }
            $relative = if ([string]::IsNullOrEmpty([string]$current.relative)) { $child.Name } else { "$($current.relative)/$($child.Name)" }
            if ($child.PSIsContainer) {
                $entries.Add("D|$relative")
                $directories.Push([pscustomobject]@{ path=$child.FullName; relative=$relative })
            } else {
                $entries.Add("F|$relative|$((Get-FileHash -LiteralPath $child.FullName -Algorithm SHA256 -ErrorAction Stop).Hash)")
            }
        }
    }
    return (@($entries | Sort-Object) -join "`n")
}

function Get-SpherewrightInstallStageInfo {
    param(
        [Parameter(Mandatory)][string]$Payload,
        [Parameter(Mandatory)][ValidateSet('plugin','mcp')][string]$Role
    )

    $payloadFull = ConvertTo-SpherewrightInstallCanonicalPath $Payload
    if ((Split-Path -Leaf $payloadFull) -cne 'payload') { throw "Prepared $Role payload must be named payload." }
    $root = Split-Path -Parent $payloadFull
    $rootItem = Get-Item -LiteralPath $root -Force -ErrorAction Stop
    if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $rootItem.PSIsContainer) {
        throw "Prepared $Role stage root must be a non-reparse directory."
    }
    $match = [regex]::Match($rootItem.Name, '^\.spherewright-stage-(?<id>[0-9a-f]{32})-' + $Role + '$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) { throw "Prepared $Role stage root has an invalid name: $($rootItem.Name)" }
    return [pscustomobject]@{ payload=$payloadFull; root=$rootItem.FullName; parent=(Split-Path -Parent $rootItem.FullName); operationId=$match.Groups['id'].Value.ToLowerInvariant() }
}

function Assert-SpherewrightInstallNonReparseAncestors {
    param([Parameter(Mandatory)][string]$Path)

    $cursor = ConvertTo-SpherewrightInstallCanonicalPath $Path
    while (-not (Test-Path -LiteralPath $cursor)) {
        $next = Split-Path -Parent $cursor
        if ([string]::IsNullOrWhiteSpace($next) -or [string]::Equals($next, $cursor, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Path has no existing filesystem ancestor: $Path"
        }
        $cursor = $next
    }
    while ($true) {
        $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
        if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Path ancestor must be a non-reparse directory: $cursor"
        }
        $parent = Split-Path -Parent $cursor
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $cursor, [StringComparison]::OrdinalIgnoreCase)) { break }
        $cursor = $parent
    }
}

function Read-SpherewrightInstallBoundedProgressRecord {
    param([Parameter(Mandatory)][string]$Path)

    $recordItem = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($recordItem.PSIsContainer -or ($recordItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Transaction progress record must be a non-reparse file: $Path"
    }
    $maximumBytes = 4MB
    $stream = [IO.File]::Open($recordItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($stream.Length -le 0 -or $stream.Length -gt $maximumBytes) {
            throw "Transaction progress record is empty or exceeds the 4 MiB bound: $Path"
        }
        $length = [int]$stream.Length
        $bytes = New-Object byte[] $length
        $offset = 0
        while ($offset -lt $length) {
            $read = $stream.Read($bytes, $offset, $length - $offset)
            if ($read -le 0) { throw "Transaction progress record changed while it was read: $Path" }
            $offset += $read
        }
    } finally {
        $stream.Dispose()
    }
    try {
        return ([Text.UTF8Encoding]::new($false, $true).GetString($bytes) | ConvertFrom-Json -ErrorAction Stop)
    } catch {
        throw "Transaction progress record is not valid UTF-8 JSON: $Path"
    }
}

function Get-SpherewrightInstallProgressRecordString {
    param(
        [Parameter(Mandatory)][object]$Record,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$RecordPath
    )

    $property = $Record.PSObject.Properties[$Name]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
        throw "Transaction progress record lacks ${Name}: $RecordPath"
    }
    return [string]$property.Value
}

function Get-SpherewrightInstallArchiveProgressInfo {
    param([Parameter(Mandatory)][object]$ArchiveItem)

    if (-not $ArchiveItem.PSIsContainer -or ($ArchiveItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Spherewright archive must be a non-reparse directory: $($ArchiveItem.FullName)"
    }
    $match = [regex]::Match($ArchiveItem.Name, '^\.spherewright-archive-(?<id>[0-9a-f]{32})-(?<role>plugin|mcp)$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) { throw "Spherewright archive name is invalid: $($ArchiveItem.FullName)" }
    $transactionRoot = Join-Path $ArchiveItem.FullName 'transaction'
    $transactionItem = Get-Item -LiteralPath $transactionRoot -Force -ErrorAction SilentlyContinue
    if ($null -eq $transactionItem -or -not $transactionItem.PSIsContainer -or ($transactionItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Spherewright archive has no safe transaction record directory: $($ArchiveItem.FullName)"
    }
    $recordPath = Join-Path $transactionRoot 'progress.json'
    $record = Read-SpherewrightInstallBoundedProgressRecord -Path $recordPath
    $operationId = $match.Groups['id'].Value.ToLowerInvariant()
    if ([int](Get-SpherewrightInstallProgressRecordString -Record $record -Name 'schemaVersion' -RecordPath $recordPath) -ne 1 -or
        (Get-SpherewrightInstallProgressRecordString -Record $record -Name 'operationId' -RecordPath $recordPath) -cne $operationId) {
        throw "Spherewright archive has an invalid transaction identity: $($ArchiveItem.FullName)"
    }
    return [pscustomobject]@{
        archive = ConvertTo-SpherewrightInstallCanonicalPath $ArchiveItem.FullName
        operationId = $operationId
        role = $match.Groups['role'].Value.ToLowerInvariant()
        recordPath = $recordPath
        record = $record
    }
}

function Assert-SpherewrightInstallNoPendingArchives {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Parent,
        [string[]]$AllowedStageRoots = @()
    )

    $parentFull = ConvertTo-SpherewrightInstallCanonicalPath $Parent
    $parentItem = Get-Item -LiteralPath $parentFull -Force -ErrorAction SilentlyContinue
    if ($null -eq $parentItem) { return }
    if (-not $parentItem.PSIsContainer -or ($parentItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Transaction parent must be a non-reparse directory: $parentFull"
    }
    Assert-SpherewrightInstallNonReparseAncestors -Path $parentFull
    $allowed = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($root in @($AllowedStageRoots)) { [void]$allowed.Add((ConvertTo-SpherewrightInstallCanonicalPath $root)) }
    foreach ($child in @(Get-ChildItem -LiteralPath $parentItem.FullName -Force -ErrorAction Stop)) {
        if ($child.Name -like '.spherewright-stage-*') {
            $childFull = ConvertTo-SpherewrightInstallCanonicalPath $child.FullName
            if (-not $allowed.Contains($childFull)) {
                throw "A prior Spherewright staging residue must be inspected before another transaction: $childFull"
            }
            continue
        }
        if ($child.Name -notlike '.spherewright-archive-*') { continue }
        $info = Get-SpherewrightInstallArchiveProgressInfo -ArchiveItem $child
        $status = Get-SpherewrightInstallProgressRecordString -Record $info.record -Name 'status' -RecordPath $info.recordPath
        if ($status -notin @('committed', 'rolled_back')) {
            throw "Spherewright archive is non-terminal and requires inspection: $($child.FullName)"
        }
        $pluginArchive = ConvertTo-SpherewrightInstallCanonicalPath (Get-SpherewrightInstallProgressRecordString -Record $info.record -Name 'pluginArchive' -RecordPath $info.recordPath)
        $mcpArchive = ConvertTo-SpherewrightInstallCanonicalPath (Get-SpherewrightInstallProgressRecordString -Record $info.record -Name 'mcpArchive' -RecordPath $info.recordPath)
        $expectedThisArchive = if ($info.role -ceq 'plugin') { $pluginArchive } else { $mcpArchive }
        $counterpartArchive = if ($info.role -ceq 'plugin') { $mcpArchive } else { $pluginArchive }
        $counterpartRole = if ($info.role -ceq 'plugin') { 'mcp' } else { 'plugin' }
        if (-not [string]::Equals($expectedThisArchive, $info.archive, [StringComparison]::OrdinalIgnoreCase) -or
            (Split-Path -Leaf $counterpartArchive) -cne ('.spherewright-archive-' + $info.operationId + '-' + $counterpartRole)) {
            throw "Spherewright archive counterpart binding is invalid: $($child.FullName)"
        }
        $counterpartItem = Get-Item -LiteralPath $counterpartArchive -Force -ErrorAction SilentlyContinue
        if ($null -eq $counterpartItem) { throw "Spherewright archive counterpart is missing: $counterpartArchive" }
        $counterpart = Get-SpherewrightInstallArchiveProgressInfo -ArchiveItem $counterpartItem
        $counterpartStatus = Get-SpherewrightInstallProgressRecordString -Record $counterpart.record -Name 'status' -RecordPath $counterpart.recordPath
        if ($counterpart.role -cne $counterpartRole -or $counterpart.operationId -cne $info.operationId -or $counterpartStatus -cne $status) {
            throw "Spherewright archive counterpart is non-terminal or inconsistent: $counterpartArchive"
        }
        foreach ($pathField in @('pluginArchive', 'mcpArchive', 'pluginDestination', 'mcpDestination')) {
            $currentValue = ConvertTo-SpherewrightInstallCanonicalPath (Get-SpherewrightInstallProgressRecordString -Record $info.record -Name $pathField -RecordPath $info.recordPath)
            $counterpartValue = ConvertTo-SpherewrightInstallCanonicalPath (Get-SpherewrightInstallProgressRecordString -Record $counterpart.record -Name $pathField -RecordPath $counterpart.recordPath)
            if (-not [string]::Equals($currentValue, $counterpartValue, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Spherewright archive counterpart disagrees about ${pathField}: $counterpartArchive"
            }
        }
    }
}

function Get-SpherewrightInstallMutexName {
    param([Parameter(Mandatory)][string]$CanonicalTarget)

    $bytes = [Text.Encoding]::UTF8.GetBytes($CanonicalTarget.ToUpperInvariant())
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($bytes)
    } finally {
        $sha.Dispose()
    }
    return 'Local\SpherewrightInstallTarget-' + ([BitConverter]::ToString($hash).Replace('-', ''))
}

function Enter-SpherewrightInstallTargetLocks {
    param([Parameter(Mandatory)][string[]]$Targets)

    $locks = [Collections.Generic.List[object]]::new()
    try {
        foreach ($target in @($Targets | Sort-Object -Unique)) {
            $mutex = [Threading.Mutex]::new($false, (Get-SpherewrightInstallMutexName $target))
            try {
                if (-not $mutex.WaitOne(0)) { throw "Another installer transaction holds the target lock: $target" }
            } catch [Threading.AbandonedMutexException] {
                throw "An abandoned installer transaction lock requires inspection; no automatic recovery: $target"
            }
            $locks.Add($mutex)
        }
        return @($locks)
    } catch {
        foreach ($mutex in @($locks)) {
            try { $mutex.ReleaseMutex() } catch { }
            $mutex.Dispose()
        }
        throw
    }
}

function Exit-SpherewrightInstallTargetLocks {
    param([object[]]$Locks = @())

    for ($lockIndex = $Locks.Count - 1; $lockIndex -ge 0; $lockIndex--) {
        $mutex = $Locks[$lockIndex]
        try { $mutex.ReleaseMutex() } catch { }
        $mutex.Dispose()
    }
}

function Invoke-SpherewrightInstallFaultHook {
    param(
        [scriptblock]$BeforeMutation,
        [Parameter(Mandatory)][string]$Step
    )

    if ($null -ne $BeforeMutation) { & $BeforeMutation $Step }
}

function Set-SpherewrightInstallJournalPaths {
    param([Parameter(Mandatory)][hashtable]$State)

    $State.pluginJournalPath = Join-Path $State.pluginTransactionRoot 'progress.json'
    $State.mcpJournalPath = Join-Path $State.mcpTransactionRoot 'progress.json'
    # Keep the singular path for the installer result contract.  The Plugin
    # journal is the durable primary record, but every update is mirrored to
    # the MCP transaction record as well.
    $State.journalPath = $State.pluginJournalPath
    $State.journalPaths = @($State.pluginJournalPath, $State.mcpJournalPath)
}

function Write-SpherewrightInstallProgressFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Json
    )

    $encoding = [Text.UTF8Encoding]::new($false)
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $writer = [IO.StreamWriter]::new($stream, $encoding, 4096, $true)
        try {
            $writer.Write($Json)
            $writer.Flush()
            # FileStream.Flush(Boolean) asks the OS to flush the file data,
            # rather than treating the journal as a best-effort log.
            $stream.Flush($true)
        } finally {
            $writer.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
}

function Write-SpherewrightInstallProgress {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][string]$Phase
    )

    $State.record['phase'] = $Phase
    $State.record['updatedUtc'] = [DateTime]::UtcNow.ToString('o')
    Invoke-SpherewrightInstallFaultHook -BeforeMutation $State.beforeMutation -Step ('progress-' + $Phase)
    $json = $State.record | ConvertTo-Json -Depth 12
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($journalPath in @($State.journalPaths)) {
        if ($seen.Add([string]$journalPath)) {
            Write-SpherewrightInstallProgressFile -Path $journalPath -Json $json
        }
    }
}

function Add-SpherewrightInstallRollbackJournalError {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][object]$ErrorRecord
    )

    if ($null -eq $State.rollbackJournalErrors) {
        $State.rollbackJournalErrors = [Collections.Generic.List[string]]::new()
    }
    $State.rollbackJournalErrors.Add([string]$ErrorRecord.Exception.Message)
}

function Write-SpherewrightInstallRollbackProgressBestEffort {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][string]$Phase
    )

    try {
        Write-SpherewrightInstallProgress -State $State -Phase $Phase
        return $true
    } catch {
        Add-SpherewrightInstallRollbackJournalError -State $State -ErrorRecord $_
        return $false
    }
}

function Invoke-SpherewrightInstallMutation {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][string]$Step,
        [Parameter(Mandatory)][scriptblock]$Action,
        [switch]$RequireJournal
    )

    if ($State.rollbackMode -and -not $RequireJournal) {
        # A failed journal write is itself unsafe and ultimately leaves an
        # inspectable needs_recovery record, but it must not prevent a normal
        # caught-failure attempt to put the old payloads back in place.
        [void](Write-SpherewrightInstallRollbackProgressBestEffort -State $State -Phase $Step)
    } else {
        Write-SpherewrightInstallProgress -State $State -Phase $Step
    }
    Invoke-SpherewrightInstallFaultHook -BeforeMutation $State.beforeMutation -Step $Step
    & $Action
}

function New-SpherewrightInstallBackupMap {
    param([Parameter(Mandatory)][object]$Snapshot)

    $result = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($Snapshot.files)) {
        if ($file.exists) { $result.Add([string]$file.relative, [string]$file.sha256) }
    }
    return $result
}

function Copy-SpherewrightInstallSnapshotToBackup {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][string]$Role,
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$BackupRoot,
        [Parameter(Mandatory)][object]$Snapshot,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles
    )

    Invoke-SpherewrightInstallMutation -State $State -Step ('create-' + $Role + '-backup-root') -Action {
        [void][IO.Directory]::CreateDirectory($BackupRoot)
    }
    foreach ($file in @($Snapshot.files | Where-Object { $_.exists } | Sort-Object relative)) {
        $relative = [string]$file.relative
        $source = Join-Path $SourceRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        $destination = Join-Path $BackupRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        $destinationParent = Split-Path -Parent $destination
        if (-not (Test-Path -LiteralPath $destinationParent -PathType Container)) {
            Invoke-SpherewrightInstallMutation -State $State -Step ('create-' + $Role + '-backup-parent-' + $relative) -Action {
                [void][IO.Directory]::CreateDirectory($destinationParent)
            }
        }
        Invoke-SpherewrightInstallMutation -State $State -Step ('backup-' + $Role + '-' + $relative) -Action {
            Copy-Item -LiteralPath $source -Destination $destination -Force -ErrorAction Stop
        }
    }
    $backupExpected = New-SpherewrightInstallBackupMap $Snapshot
    $null = Assert-SpherewrightInstallSnapshotComplete -Root $BackupRoot -ExpectedFiles $backupExpected
}

function Move-SpherewrightInstallLiveFileToFailure {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][string]$Role,
        [Parameter(Mandatory)][string]$LiveRoot,
        [Parameter(Mandatory)][string]$FailureRoot,
        [Parameter(Mandatory)][string]$Relative
    )

    $source = Join-Path $LiveRoot ($Relative -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { return }
    $destination = Join-Path $FailureRoot ($Relative -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (Test-Path -LiteralPath $destination) { throw "Failure evidence path already exists: $destination" }
    $parent = Split-Path -Parent $destination
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        Invoke-SpherewrightInstallMutation -State $State -Step ('create-' + $Role + '-failure-parent-' + $Relative) -Action {
            [void][IO.Directory]::CreateDirectory($parent)
        }
    }
    Invoke-SpherewrightInstallMutation -State $State -Step ('rollback-move-' + $Role + '-new-' + $Relative) -Action {
        [IO.File]::Move($source, $destination)
    }
}

function Restore-SpherewrightInstallPluginOtherFiles {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][object]$OriginalSnapshot,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles
    )

    $main = $script:SpherewrightInstallTransactionMainPluginFile
    foreach ($original in @($OriginalSnapshot.files | Where-Object { $_.relative -cne $main } | Sort-Object relative -Descending)) {
        $relative = [string]$original.relative
        $live = Join-Path $State.pluginDestination ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        $alreadyOriginal = $false
        if ($original.exists -and (Test-Path -LiteralPath $live -PathType Leaf)) {
            $currentHash = (Get-FileHash -LiteralPath $live -Algorithm SHA256 -ErrorAction Stop).Hash
            $alreadyOriginal = [string]::Equals($currentHash, [string]$original.sha256, [StringComparison]::OrdinalIgnoreCase)
        }
        if (-not $alreadyOriginal) {
            Move-SpherewrightInstallLiveFileToFailure -State $State -Role 'plugin' -LiveRoot $State.pluginDestination -FailureRoot $State.pluginFailureRoot -Relative $relative
        }
        if ($original.exists -and -not $alreadyOriginal) {
            $backup = Join-Path $State.pluginBackupRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
            $parent = Split-Path -Parent $live
            if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
                Invoke-SpherewrightInstallMutation -State $State -Step ('rollback-create-plugin-parent-' + $relative) -Action {
                    [void][IO.Directory]::CreateDirectory($parent)
                }
            }
            Invoke-SpherewrightInstallMutation -State $State -Step ('rollback-copy-plugin-old-' + $relative) -Action {
                Copy-Item -LiteralPath $backup -Destination $live -Force -ErrorAction Stop
            }
        }
    }
}

function Quiesce-SpherewrightInstallPluginMainForRollback {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][object]$OriginalSnapshot
    )

    $main = $script:SpherewrightInstallTransactionMainPluginFile
    $original = @($OriginalSnapshot.files | Where-Object { $_.relative -ceq $main })
    if ($original.Count -ne 1) { throw 'Original Plugin snapshot does not contain the main Plugin file.' }
    $live = Join-Path $State.pluginDestination $main
    if (-not (Test-Path -LiteralPath $live -PathType Leaf)) { return }
    if ($original[0].exists) {
        $currentHash = (Get-FileHash -LiteralPath $live -Algorithm SHA256 -ErrorAction Stop).Hash
        if ([string]::Equals($currentHash, [string]$original[0].sha256, [StringComparison]::OrdinalIgnoreCase)) { return }
    }
    # Do not put old dependencies back while a promoted main Plugin DLL can
    # still be discovered by BepInEx.  A failure here intentionally prevents
    # any dependency restoration and leaves the record in needs_recovery.
    Move-SpherewrightInstallLiveFileToFailure -State $State -Role 'plugin' -LiveRoot $State.pluginDestination -FailureRoot $State.pluginFailureRoot -Relative $main
}

function Restore-SpherewrightInstallMcpPayload {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][object]$OriginalSnapshot,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$ExpectedFiles
    )

    if (Test-SpherewrightInstallSnapshotMatches -ExpectedSnapshot $OriginalSnapshot -Root $State.mcpDestination -ExpectedFiles $ExpectedFiles) { return }
    if (Test-Path -LiteralPath $State.mcpDestination) {
        if (Test-Path -LiteralPath $State.mcpFailureRoot) { throw "Failure evidence directory already exists: $($State.mcpFailureRoot)" }
        Invoke-SpherewrightInstallMutation -State $State -Step 'rollback-move-mcp-new-directory' -Action {
            [IO.Directory]::Move($State.mcpDestination, $State.mcpFailureRoot)
        }
    }
    if (-not $OriginalSnapshot.rootExisted) { return }
    if (Test-Path -LiteralPath $State.mcpPriorLiveRoot) {
        Invoke-SpherewrightInstallMutation -State $State -Step 'rollback-move-mcp-prior-directory' -Action {
            [IO.Directory]::Move($State.mcpPriorLiveRoot, $State.mcpDestination)
        }
        return
    }
    Invoke-SpherewrightInstallMutation -State $State -Step 'rollback-create-mcp-old-root' -Action {
        [void][IO.Directory]::CreateDirectory($State.mcpDestination)
    }
    foreach ($original in @($OriginalSnapshot.files | Where-Object { $_.exists } | Sort-Object relative)) {
        $relative = [string]$original.relative
        $source = Join-Path $State.mcpBackupRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        $destination = Join-Path $State.mcpDestination ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        $parent = Split-Path -Parent $destination
        if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
            Invoke-SpherewrightInstallMutation -State $State -Step ('rollback-create-mcp-parent-' + $relative) -Action {
                [void][IO.Directory]::CreateDirectory($parent)
            }
        }
        Invoke-SpherewrightInstallMutation -State $State -Step ('rollback-copy-mcp-old-' + $relative) -Action {
            Copy-Item -LiteralPath $source -Destination $destination -Force -ErrorAction Stop
        }
    }
}

function Restore-SpherewrightInstallPluginMainFile {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][object]$OriginalSnapshot
    )

    $main = $script:SpherewrightInstallTransactionMainPluginFile
    $original = @($OriginalSnapshot.files | Where-Object { $_.relative -ceq $main })
    if ($original.Count -ne 1) { throw 'Original Plugin snapshot does not contain the main Plugin file.' }
    $live = Join-Path $State.pluginDestination $main
    $alreadyOriginal = $false
    if ($original[0].exists -and (Test-Path -LiteralPath $live -PathType Leaf)) {
        $currentHash = (Get-FileHash -LiteralPath $live -Algorithm SHA256 -ErrorAction Stop).Hash
        $alreadyOriginal = [string]::Equals($currentHash, [string]$original[0].sha256, [StringComparison]::OrdinalIgnoreCase)
    }
    if (-not $alreadyOriginal) {
        Move-SpherewrightInstallLiveFileToFailure -State $State -Role 'plugin' -LiveRoot $State.pluginDestination -FailureRoot $State.pluginFailureRoot -Relative $main
    }
    if ($original[0].exists -and -not $alreadyOriginal) {
        if (Test-Path -LiteralPath $State.pluginPriorMain) {
            Invoke-SpherewrightInstallMutation -State $State -Step 'rollback-move-plugin-prior-main' -Action {
                [IO.File]::Move($State.pluginPriorMain, $live)
            }
        } else {
            $backup = Join-Path $State.pluginBackupRoot $main
            Invoke-SpherewrightInstallMutation -State $State -Step 'rollback-copy-plugin-old-main' -Action {
                Copy-Item -LiteralPath $backup -Destination $live -Force -ErrorAction Stop
            }
        }
    }
}

function Restore-SpherewrightInstallOriginalPayloads {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][object]$PluginOriginal,
        [Parameter(Mandatory)][object]$McpOriginal,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$PluginExpectedFiles,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string,string]]$McpExpectedFiles,
        [Parameter(Mandatory)][string]$HandoffFingerprint
    )

    Quiesce-SpherewrightInstallPluginMainForRollback -State $State -OriginalSnapshot $PluginOriginal
    Restore-SpherewrightInstallPluginOtherFiles -State $State -OriginalSnapshot $PluginOriginal -ExpectedFiles $PluginExpectedFiles
    Restore-SpherewrightInstallMcpPayload -State $State -OriginalSnapshot $McpOriginal -ExpectedFiles $McpExpectedFiles
    Restore-SpherewrightInstallPluginMainFile -State $State -OriginalSnapshot $PluginOriginal
    if (-not $PluginOriginal.rootExisted -and (Test-Path -LiteralPath $State.pluginDestination -PathType Container)) {
        $children = @(Get-ChildItem -LiteralPath $State.pluginDestination -Force -ErrorAction Stop)
        if ($children.Count -eq 0) {
            Invoke-SpherewrightInstallMutation -State $State -Step 'rollback-remove-empty-plugin-root' -Action {
                [IO.Directory]::Delete($State.pluginDestination, $false)
            }
        }
    }
    if (-not (Test-SpherewrightInstallSnapshotMatches -ExpectedSnapshot $PluginOriginal -Root $State.pluginDestination -ExpectedFiles $PluginExpectedFiles -PreservedDirectories @('runtime-handoff'))) {
        throw 'Rollback did not restore the exact original Plugin payload state.'
    }
    if (-not (Test-SpherewrightInstallSnapshotMatches -ExpectedSnapshot $McpOriginal -Root $State.mcpDestination -ExpectedFiles $McpExpectedFiles)) {
        throw 'Rollback did not restore the exact original MCP payload state.'
    }
    if (-not [string]::Equals((Get-SpherewrightInstallTreeFingerprint (Join-Path $State.pluginDestination 'runtime-handoff')), $HandoffFingerprint, [StringComparison]::Ordinal)) {
        throw 'Rollback changed the protected runtime-handoff tree.'
    }
}

function Complete-SpherewrightInstallTerminalArchive {
    param([Parameter(Mandatory)][hashtable]$State)

    if ([string]::Equals($State.mcpDataRoot, $State.mcpStageRoot, [StringComparison]::OrdinalIgnoreCase)) {
        Invoke-SpherewrightInstallMutation -State $State -Step 'archive-mcp-stage' -RequireJournal -Action {
            [IO.Directory]::Move($State.mcpStageRoot, $State.mcpArchive)
            $State.mcpDataRoot = $State.mcpArchive
            $State.mcpTransactionRoot = Join-Path $State.mcpDataRoot 'transaction'
            $State.mcpBackupRoot = Join-Path $State.mcpTransactionRoot 'old-payload'
            $State.mcpPriorLiveRoot = Join-Path $State.mcpTransactionRoot 'prior-live'
            $State.mcpFailureRoot = Join-Path $State.mcpTransactionRoot 'failed-new-live'
            Set-SpherewrightInstallJournalPaths -State $State
        }
        # The MCP root has moved.  Persist the same terminal/current state at
        # its new location before attempting to archive the Plugin root.
        Write-SpherewrightInstallProgress -State $State -Phase 'archived-mcp-stage'
    }
    if ([string]::Equals($State.pluginDataRoot, $State.pluginStageRoot, [StringComparison]::OrdinalIgnoreCase)) {
        Invoke-SpherewrightInstallMutation -State $State -Step 'archive-plugin-stage' -RequireJournal -Action {
            [IO.Directory]::Move($State.pluginStageRoot, $State.pluginArchive)
            $State.pluginDataRoot = $State.pluginArchive
            $State.pluginTransactionRoot = Join-Path $State.pluginDataRoot 'transaction'
            $State.pluginBackupRoot = Join-Path $State.pluginTransactionRoot 'old-payload'
            $State.pluginPriorMain = Join-Path $State.pluginTransactionRoot 'prior-main.dll'
            $State.pluginFailureRoot = Join-Path $State.pluginTransactionRoot 'failed-new'
            Set-SpherewrightInstallJournalPaths -State $State
        }
        # Both records now live beneath terminal archive roots.  This write is
        # deliberately after the rename so a future residue gate can inspect
        # either archive without relying on a stale path.
        Write-SpherewrightInstallProgress -State $State -Phase 'archived-plugin-stage'
    }
}

function Get-SpherewrightInstallParentCreationPlan {
    param(
        [Parameter(Mandatory)][string]$McpDestination,
        [string[]]$CreatedParentDirectories = @()
    )

    $destination = ConvertTo-SpherewrightInstallCanonicalPath $McpDestination
    $parent = Split-Path -Parent $destination
    if ([string]::IsNullOrWhiteSpace($parent)) { throw 'MCP destination must have a concrete parent directory.' }
    $missing = [Collections.Generic.List[string]]::new()
    $cursor = $parent
    while (-not (Test-Path -LiteralPath $cursor)) {
        $missing.Add((ConvertTo-SpherewrightInstallCanonicalPath $cursor))
        $next = Split-Path -Parent $cursor
        if ([string]::IsNullOrWhiteSpace($next) -or [string]::Equals($next, $cursor, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'MCP destination has no existing non-reparse ancestor.'
        }
        $cursor = $next
    }
    $existingAncestor = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
    if (($existingAncestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $existingAncestor.PSIsContainer) {
        throw 'MCP destination ancestor must be a non-reparse directory.'
    }
    $required = [Collections.Generic.List[string]]::new()
    for ($index = $missing.Count - 1; $index -ge 0; $index--) { $required.Add($missing[$index]) }
    $providedList = [Collections.Generic.List[string]]::new()
    foreach ($providedParent in $CreatedParentDirectories) {
        if ($null -eq $providedParent) { continue }
        $providedList.Add((ConvertTo-SpherewrightInstallCanonicalPath ([string]$providedParent)))
    }
    $provided = @($providedList)
    if ($provided.Count -ne $required.Count) { throw 'CreatedParentDirectories must exactly enumerate the currently missing MCP destination ancestors.' }
    for ($index = 0; $index -lt $required.Count; $index++) {
        if (-not [string]::Equals($provided[$index], $required[$index], [StringComparison]::OrdinalIgnoreCase)) {
            throw 'CreatedParentDirectories must be ordered from the nearest existing ancestor to the MCP destination parent.'
        }
        if (Test-Path -LiteralPath $provided[$index]) { throw 'CreatedParentDirectories must not include an existing directory.' }
    }
    return @($required)
}

function Get-SpherewrightInstallExistingAncestorDirectories {
    param([Parameter(Mandatory)][string]$Path)

    $cursor = ConvertTo-SpherewrightInstallCanonicalPath $Path
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    while ($true) {
        $item = Get-Item -LiteralPath $cursor -Force -ErrorAction SilentlyContinue
        if ($null -ne $item) {
            if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw "Transaction ancestor must be a non-reparse directory: $cursor"
            }
            if ($seen.Add($item.FullName)) { Write-Output $item.FullName }
        }
        $parent = Split-Path -Parent $cursor
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $cursor, [StringComparison]::OrdinalIgnoreCase)) { break }
        $cursor = $parent
    }
}

function Assert-SpherewrightInstallNoPendingTransactionResidue {
    param(
        [Parameter(Mandatory)][string]$PluginDestination,
        [Parameter(Mandatory)][string]$McpDestination,
        [Parameter(Mandatory)][string[]]$AllowedStageRoots
    )

    $parents = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in @($PluginDestination, $McpDestination)) {
        foreach ($ancestor in @(Get-SpherewrightInstallExistingAncestorDirectories -Path $path)) {
            [void]$parents.Add($ancestor)
        }
    }
    foreach ($parent in @($parents | Sort-Object)) {
        Assert-SpherewrightInstallNoPendingArchives -Parent $parent -AllowedStageRoots $AllowedStageRoots
    }
}

function Invoke-SpherewrightInstallTransaction {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$PluginStagePayload,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$PluginDestination,
        [Parameter(Mandatory)][object]$PluginExpectedFiles,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$McpStagePayload,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$McpDestination,
        [Parameter(Mandatory)][object]$McpExpectedFiles,
        [Parameter(Mandatory)][scriptblock]$VerifyInstalled,
        [Alias('ParentDirectoriesToCreate')][string[]]$CreatedParentDirectories = @(),
        [scriptblock]$BeforeMutation
    )

    $pluginExpected = New-SpherewrightInstallExpectedMap $PluginExpectedFiles
    $mcpExpected = New-SpherewrightInstallExpectedMap $McpExpectedFiles
    if (-not $pluginExpected.ContainsKey($script:SpherewrightInstallTransactionMainPluginFile)) {
        throw "Plugin expected files must include $($script:SpherewrightInstallTransactionMainPluginFile)."
    }
    $pluginStageInfo = Get-SpherewrightInstallStageInfo -Payload $PluginStagePayload -Role plugin
    $mcpStageInfo = Get-SpherewrightInstallStageInfo -Payload $McpStagePayload -Role mcp
    if ($pluginStageInfo.operationId -cne $mcpStageInfo.operationId) { throw 'Plugin and MCP stage operation IDs must match.' }
    $pluginDestinationFull = ConvertTo-SpherewrightInstallCanonicalPath $PluginDestination
    $mcpDestinationFull = ConvertTo-SpherewrightInstallCanonicalPath $McpDestination
    foreach ($transactionPath in @($pluginStageInfo.payload, $mcpStageInfo.payload, $pluginDestinationFull, $mcpDestinationFull)) {
        Assert-SpherewrightInstallNonReparseAncestors -Path $transactionPath
    }
    $parentCreationPlan = @(Get-SpherewrightInstallParentCreationPlan -McpDestination $mcpDestinationFull -CreatedParentDirectories $CreatedParentDirectories)
    foreach ($path in @($pluginDestinationFull, $mcpDestinationFull, $pluginStageInfo.root, $mcpStageInfo.root)) {
        $item = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Reparse-point transaction path is forbidden: $path" }
    }
    if (Test-SpherewrightInstallPathOverlap $pluginDestinationFull $mcpDestinationFull) { throw 'Plugin and MCP live destinations must not overlap.' }
    foreach ($pair in @(
        @($pluginStageInfo.root, $pluginDestinationFull), @($pluginStageInfo.root, $mcpDestinationFull),
        @($mcpStageInfo.root, $pluginDestinationFull), @($mcpStageInfo.root, $mcpDestinationFull),
        @($pluginStageInfo.root, $mcpStageInfo.root)
    )) {
        if (Test-SpherewrightInstallPathOverlap $pair[0] $pair[1]) { throw 'Prepared stage and live destination paths must not overlap.' }
    }
    if (-not [string]::Equals([IO.Path]::GetPathRoot($mcpStageInfo.payload), [IO.Path]::GetPathRoot($mcpDestinationFull), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'MCP staged payload and live destination must be on the same volume for directory swap.'
    }
    if (-not [string]::Equals([IO.Path]::GetPathRoot($pluginStageInfo.payload), [IO.Path]::GetPathRoot($pluginDestinationFull), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Plugin staged payload and live destination must be on the same volume for the main Plugin file move.'
    }
    if (Test-Path -LiteralPath (Join-Path $pluginStageInfo.root 'transaction')) { throw 'Plugin stage already contains a transaction record; do not automatically recover it.' }
    if (Test-Path -LiteralPath (Join-Path $mcpStageInfo.root 'transaction')) { throw 'MCP stage already contains a transaction record; do not automatically recover it.' }

    $pluginArchive = Join-Path $pluginStageInfo.parent ('.spherewright-archive-' + $pluginStageInfo.operationId + '-plugin')
    $mcpArchive = Join-Path $mcpStageInfo.parent ('.spherewright-archive-' + $mcpStageInfo.operationId + '-mcp')
    if ((Test-Path -LiteralPath $pluginArchive) -or (Test-Path -LiteralPath $mcpArchive)) {
        throw 'Terminal archive path already exists; do not overwrite a prior transaction.'
    }

    $locks = @()
    $state = $null
    $liveMutationStarted = $false
    $recordCreated = $false
    try {
        $locks = Enter-SpherewrightInstallTargetLocks -Targets @($pluginDestinationFull, $mcpDestinationFull)
        Assert-SpherewrightInstallNoPendingTransactionResidue -PluginDestination $pluginDestinationFull -McpDestination $mcpDestinationFull -AllowedStageRoots @($pluginStageInfo.root, $mcpStageInfo.root)
        $pluginStaged = Assert-SpherewrightInstallSnapshotComplete -Root $pluginStageInfo.payload -ExpectedFiles $pluginExpected
        $mcpStaged = Assert-SpherewrightInstallSnapshotComplete -Root $mcpStageInfo.payload -ExpectedFiles $mcpExpected
        $pluginOriginal = Get-SpherewrightInstallPayloadSnapshot -Root $pluginDestinationFull -ExpectedFiles $pluginExpected -PreservedDirectories @('runtime-handoff')
        $mcpOriginal = Get-SpherewrightInstallPayloadSnapshot -Root $mcpDestinationFull -ExpectedFiles $mcpExpected
        $handoffFingerprint = Get-SpherewrightInstallTreeFingerprint (Join-Path $pluginDestinationFull 'runtime-handoff')

        $state = @{
            operationId = $pluginStageInfo.operationId
            beforeMutation = $BeforeMutation
            pluginStageRoot = $pluginStageInfo.root
            mcpStageRoot = $mcpStageInfo.root
            pluginDataRoot = $pluginStageInfo.root
            mcpDataRoot = $mcpStageInfo.root
            pluginDestination = $pluginDestinationFull
            mcpDestination = $mcpDestinationFull
            pluginArchive = $pluginArchive
            mcpArchive = $mcpArchive
            pluginTransactionRoot = Join-Path $pluginStageInfo.root 'transaction'
            mcpTransactionRoot = Join-Path $mcpStageInfo.root 'transaction'
            rollbackMode = $false
            rollbackJournalErrors = [Collections.Generic.List[string]]::new()
        }
        $state.pluginBackupRoot = Join-Path $state.pluginTransactionRoot 'old-payload'
        $state.mcpBackupRoot = Join-Path $state.mcpTransactionRoot 'old-payload'
        $state.pluginPriorMain = Join-Path $state.pluginTransactionRoot 'prior-main.dll'
        $state.mcpPriorLiveRoot = Join-Path $state.mcpTransactionRoot 'prior-live'
        $state.pluginFailureRoot = Join-Path $state.pluginTransactionRoot 'failed-new'
        $state.mcpFailureRoot = Join-Path $state.mcpTransactionRoot 'failed-new-live'
        Set-SpherewrightInstallJournalPaths -State $state

        Invoke-SpherewrightInstallFaultHook -BeforeMutation $BeforeMutation -Step 'create-plugin-transaction-root'
        [void][IO.Directory]::CreateDirectory($state.pluginTransactionRoot)
        Invoke-SpherewrightInstallFaultHook -BeforeMutation $BeforeMutation -Step 'create-mcp-transaction-root'
        [void][IO.Directory]::CreateDirectory($state.mcpTransactionRoot)

        $state.record = @{
            schemaVersion = 1
            operationId = $state.operationId
            status = 'pending'
            phase = 'created'
            createdUtc = [DateTime]::UtcNow.ToString('o')
            updatedUtc = [DateTime]::UtcNow.ToString('o')
            pluginDestination = $pluginDestinationFull
            mcpDestination = $mcpDestinationFull
            pluginArchive = $pluginArchive
            mcpArchive = $mcpArchive
            plannedParentDirectories = @($parentCreationPlan)
            createdParentDirectories = @()
            pluginOriginal = $pluginOriginal
            mcpOriginal = $mcpOriginal
            pluginExpectedFiles = @($pluginExpected.Keys | Sort-Object | ForEach-Object { [pscustomobject]@{relative=$_;sha256=$pluginExpected[$_]} })
            mcpExpectedFiles = @($mcpExpected.Keys | Sort-Object | ForEach-Object { [pscustomobject]@{relative=$_;sha256=$mcpExpected[$_]} })
            runtimeHandoffFingerprint = $handoffFingerprint
            caughtFailureRollbackSupported = $true
            crashRecoverySupported = $false
        }
        Write-SpherewrightInstallProgress -State $state -Phase 'pending'
        $recordCreated = $true

        Copy-SpherewrightInstallSnapshotToBackup -State $state -Role plugin -SourceRoot $pluginDestinationFull -BackupRoot $state.pluginBackupRoot -Snapshot $pluginOriginal -ExpectedFiles $pluginExpected
        Copy-SpherewrightInstallSnapshotToBackup -State $state -Role mcp -SourceRoot $mcpDestinationFull -BackupRoot $state.mcpBackupRoot -Snapshot $mcpOriginal -ExpectedFiles $mcpExpected
        $pluginBackupExpected = New-SpherewrightInstallBackupMap $pluginOriginal
        $mcpBackupExpected = New-SpherewrightInstallBackupMap $mcpOriginal
        Write-SpherewrightInstallProgress -State $state -Phase 'backups-verified'

        for ($parentIndex = 0; $parentIndex -lt $parentCreationPlan.Count; $parentIndex++) {
            $plannedParent = $parentCreationPlan[$parentIndex]
            if (Test-Path -LiteralPath $plannedParent) { throw "Planned MCP parent unexpectedly exists: $plannedParent" }
            Invoke-SpherewrightInstallMutation -State $state -Step ('create-mcp-parent-' + $parentIndex) -Action {
                [void][IO.Directory]::CreateDirectory($plannedParent)
            }
            $state.record['createdParentDirectories'] = @($state.record['createdParentDirectories']) + @($plannedParent)
            Write-SpherewrightInstallProgress -State $state -Phase ('created-mcp-parent-' + $parentIndex)
        }

        # Everything that will be relied upon for promotion is sampled again
        # after backup and parent creation.  A concurrent change in this window
        # is a pre-live failure, not a reason to restore an obsolete snapshot.
        foreach ($transactionPath in @($pluginStageInfo.payload, $mcpStageInfo.payload, $pluginDestinationFull, $mcpDestinationFull)) {
            Assert-SpherewrightInstallNonReparseAncestors -Path $transactionPath
        }
        if (-not (Test-SpherewrightInstallSnapshotMatches -ExpectedSnapshot $pluginOriginal -Root $pluginDestinationFull -ExpectedFiles $pluginExpected -PreservedDirectories @('runtime-handoff'))) {
            throw 'Plugin live payload changed after backup and before promotion.'
        }
        if (-not (Test-SpherewrightInstallSnapshotMatches -ExpectedSnapshot $mcpOriginal -Root $mcpDestinationFull -ExpectedFiles $mcpExpected)) {
            throw 'MCP live payload changed after backup and before promotion.'
        }
        if ($pluginBackupExpected.Count -gt 0) {
            $null = Assert-SpherewrightInstallSnapshotComplete -Root $state.pluginBackupRoot -ExpectedFiles $pluginBackupExpected
        }
        if ($mcpBackupExpected.Count -gt 0) {
            $null = Assert-SpherewrightInstallSnapshotComplete -Root $state.mcpBackupRoot -ExpectedFiles $mcpBackupExpected
        }
        $null = Assert-SpherewrightInstallSnapshotComplete -Root $pluginStageInfo.payload -ExpectedFiles $pluginExpected
        $null = Assert-SpherewrightInstallSnapshotComplete -Root $mcpStageInfo.payload -ExpectedFiles $mcpExpected
        if (-not [string]::Equals((Get-SpherewrightInstallTreeFingerprint (Join-Path $pluginDestinationFull 'runtime-handoff')), $handoffFingerprint, [StringComparison]::Ordinal)) {
            throw 'Protected runtime-handoff changed after backup and before promotion.'
        }

        # Recheck immediately before the first live mutation. This module never starts,
        # stops, or otherwise controls DSPGAME.
        if (Get-Process -Name 'DSPGAME' -ErrorAction SilentlyContinue) {
            throw 'DSPGAME is running. Exit the game before transaction promotion.'
        }
        $liveMutationStarted = $true
        $main = $script:SpherewrightInstallTransactionMainPluginFile
        $originalMain = @($pluginOriginal.files | Where-Object { $_.relative -ceq $main })
        if ($originalMain.Count -ne 1) { throw 'Original Plugin snapshot lacks the main Plugin file.' }
        if ($originalMain[0].exists) {
            Invoke-SpherewrightInstallMutation -State $state -Step 'move-plugin-main-outside-scan' -Action {
                [IO.File]::Move((Join-Path $pluginDestinationFull $main), $state.pluginPriorMain)
            }
        }
        if ($mcpOriginal.rootExisted) {
            Invoke-SpherewrightInstallMutation -State $state -Step 'move-mcp-live-to-prior' -Action {
                [IO.Directory]::Move($mcpDestinationFull, $state.mcpPriorLiveRoot)
            }
        }
        Invoke-SpherewrightInstallMutation -State $state -Step 'move-mcp-stage-to-live' -Action {
            [IO.Directory]::Move($mcpStageInfo.payload, $mcpDestinationFull)
        }
        if (-not (Test-Path -LiteralPath $pluginDestinationFull -PathType Container)) {
            Invoke-SpherewrightInstallMutation -State $state -Step 'create-plugin-live-root' -Action {
                [void][IO.Directory]::CreateDirectory($pluginDestinationFull)
            }
        }
        foreach ($relative in @($pluginExpected.Keys | Where-Object { $_ -cne $main } | Sort-Object)) {
            $source = Join-Path $pluginStageInfo.payload ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
            $destination = Join-Path $pluginDestinationFull ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
            $parent = Split-Path -Parent $destination
            if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
                Invoke-SpherewrightInstallMutation -State $state -Step ('create-plugin-live-parent-' + $relative) -Action {
                    [void][IO.Directory]::CreateDirectory($parent)
                }
            }
            Invoke-SpherewrightInstallMutation -State $state -Step ('copy-plugin-' + $relative) -Action {
                Copy-Item -LiteralPath $source -Destination $destination -Force -ErrorAction Stop
            }
        }
        Invoke-SpherewrightInstallMutation -State $state -Step 'copy-plugin-main-last' -Action {
            Copy-Item -LiteralPath (Join-Path $pluginStageInfo.payload $main) -Destination (Join-Path $pluginDestinationFull $main) -Force -ErrorAction Stop
        }

        Write-SpherewrightInstallProgress -State $state -Phase 'verify-installed'
        $verificationMetadata = & $VerifyInstalled $mcpDestinationFull
        $null = $verificationMetadata
        $null = Assert-SpherewrightInstallSnapshotComplete -Root $pluginDestinationFull -ExpectedFiles $pluginExpected -PreservedDirectories @('runtime-handoff')
        $null = Assert-SpherewrightInstallSnapshotComplete -Root $mcpDestinationFull -ExpectedFiles $mcpExpected
        if (-not [string]::Equals((Get-SpherewrightInstallTreeFingerprint (Join-Path $pluginDestinationFull 'runtime-handoff')), $handoffFingerprint, [StringComparison]::Ordinal)) {
            throw 'Transaction promotion changed the protected runtime-handoff tree.'
        }

        # Archive roots remain explicitly non-terminal until both payload
        # roots and their mirrored records have reached their final locations.
        # If the process stops in this interval, the residue gate fails closed.
        $state.record['status'] = 'finalizing'
        Write-SpherewrightInstallProgress -State $state -Phase 'finalizing-commit'
        Complete-SpherewrightInstallTerminalArchive -State $state
        $state.record['status'] = 'committed'
        Write-SpherewrightInstallProgress -State $state -Phase 'committed'
        return [pscustomobject][ordered]@{
            installed = $true
            status = 'committed'
            operationId = $state.operationId
            recordPath = $state.journalPath
            pluginArchive = $state.pluginArchive
            mcpArchive = $state.mcpArchive
            caughtFailureRollbackSupported = $true
            crashRecoverySupported = $false
            transactionalUpgrade = $false
        }
    } catch {
        $forwardFailure = $_
        if (-not $liveMutationStarted -or $null -eq $state) {
            if ($recordCreated) {
                try {
                    $state.record['status'] = 'failed_before_live'
                    Write-SpherewrightInstallProgress -State $state -Phase 'failed-before-live'
                } catch { }
            }
            throw $forwardFailure
        }

        $rollbackFailure = $null
        $state.rollbackMode = $true
        try {
            $state.record['status'] = 'rolling_back'
            [void](Write-SpherewrightInstallRollbackProgressBestEffort -State $state -Phase 'rolling-back')
            Restore-SpherewrightInstallOriginalPayloads -State $state -PluginOriginal $pluginOriginal -McpOriginal $mcpOriginal -PluginExpectedFiles $pluginExpected -McpExpectedFiles $mcpExpected -HandoffFingerprint $handoffFingerprint
            if ($state.rollbackJournalErrors.Count -gt 0) {
                throw [IO.IOException]::new('Rollback restored payloads but one or more durable journal writes failed.')
            }
            $state.record['status'] = 'finalizing'
            if (-not (Write-SpherewrightInstallRollbackProgressBestEffort -State $state -Phase 'finalizing-rollback')) {
                throw [IO.IOException]::new('Rollback terminal preparation could not be durably recorded.')
            }
            Complete-SpherewrightInstallTerminalArchive -State $state
            if ($state.rollbackJournalErrors.Count -gt 0) {
                throw [IO.IOException]::new('Rollback archive progress could not be durably recorded.')
            }
            $state.record['status'] = 'rolled_back'
            if (-not (Write-SpherewrightInstallRollbackProgressBestEffort -State $state -Phase 'rolled-back')) {
                throw [IO.IOException]::new('Rollback terminal state could not be durably recorded.')
            }
        } catch {
            $rollbackFailure = $_
        }
        $state.rollbackMode = $false
        if ($null -eq $rollbackFailure) {
            throw [InvalidOperationException]::new("Transaction promotion failed and both old payloads were restored: $($forwardFailure.Exception.Message)", $forwardFailure.Exception)
        }
        $journalFailure = $null
        try {
            $state.record['status'] = 'needs_recovery'
            $state.rollbackMode = $true
            [void](Write-SpherewrightInstallRollbackProgressBestEffort -State $state -Phase 'needs-recovery')
        } catch {
            $journalFailure = $_.Exception.Message
        } finally {
            $state.rollbackMode = $false
        }
        $journalMessages = @($state.rollbackJournalErrors | Select-Object -Unique)
        if ($journalMessages.Count -gt 0) {
            $journalFailure = (($journalFailure, ($journalMessages -join ' | ') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | ')
        }
        $suffix = if ($journalFailure) { " Durable recovery record update also failed: $journalFailure" } else { '' }
        throw [InvalidOperationException]::new("Transaction promotion failed and rollback failed; needs_recovery. Forward: $($forwardFailure.Exception.Message) Rollback: $($rollbackFailure.Exception.Message)$suffix", $rollbackFailure.Exception)
    } finally {
        Exit-SpherewrightInstallTargetLocks -Locks $locks
    }
}
