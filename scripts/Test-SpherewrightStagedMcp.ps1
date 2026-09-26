Set-StrictMode -Version Latest

$script:SpherewrightStagedMcpOpeningMovementUri = 'spherewright://agent/playbooks/opening-movement-v1'
$script:SpherewrightStagedMcpRequestTimeoutMilliseconds = 5000
$script:SpherewrightStagedMcpTotalDeadlineMilliseconds = 30000
$script:SpherewrightStagedMcpStdoutCharacterLimit = 524288
$script:SpherewrightStagedMcpStderrCharacterLimit = 131072

function Initialize-SpherewrightStagedMcpBoundedReaders {
    if ($null -ne ('Spherewright.StagedMcpProbe.BoundedLineReader' -as [type])) { return }

    Add-Type -TypeDefinition @'
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spherewright.StagedMcpProbe
{
    public sealed class BoundedLineReader : IDisposable
    {
        private readonly StreamReader reader;
        private readonly BlockingCollection<string> lines = new BlockingCollection<string>(new ConcurrentQueue<string>());
        private readonly int maximumCharacters;
        private readonly string label;
        private readonly Task pump;
        private volatile Exception fault;
        private volatile bool completed;
        private int characterCount;

        public BoundedLineReader(Stream stream, int maximumCharacters, string label)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (maximumCharacters <= 0) throw new ArgumentOutOfRangeException("maximumCharacters");
            this.maximumCharacters = maximumCharacters;
            this.label = label ?? "stream";
            reader = new StreamReader(stream, new UTF8Encoding(false, true), false, 4096, true);
            pump = Task.Run(async () => await PumpAsync().ConfigureAwait(false));
        }

        private async Task PumpAsync()
        {
            var buffer = new char[4096];
            var current = new StringBuilder();
            try
            {
                while (true)
                {
                    var read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (read == 0) break;
                    for (var index = 0; index < read; index++)
                    {
                        if (Interlocked.Increment(ref characterCount) > maximumCharacters)
                            throw new InvalidOperationException(label + " exceeded the maximum of " + maximumCharacters + " characters.");
                        var character = buffer[index];
                        if (character == '\n')
                        {
                            if (current.Length > 0 && current[current.Length - 1] == '\r') current.Length--;
                            lines.Add(current.ToString());
                            current.Clear();
                        }
                        else
                        {
                            current.Append(character);
                        }
                    }
                }
                if (current.Length > 0) lines.Add(current.ToString());
            }
            catch (Exception exception)
            {
                fault = exception;
            }
            finally
            {
                lines.CompleteAdding();
                completed = true;
            }
        }

        public string ReadLine(int timeoutMilliseconds)
        {
            string line;
            if (lines.TryTake(out line, timeoutMilliseconds)) return line;
            if (fault != null) throw new InvalidOperationException(label + " reader failed: " + fault.Message, fault);
            if (completed) return null;
            throw new TimeoutException(label + " did not produce a complete protocol line before its deadline.");
        }

        public bool WaitForCompletion(int timeoutMilliseconds) { return pump.Wait(timeoutMilliseconds); }

        public void ThrowIfFaulted()
        {
            if (fault != null) throw new InvalidOperationException(label + " reader failed: " + fault.Message, fault);
        }

        public string[] DrainLines()
        {
            if (!completed) throw new InvalidOperationException(label + " has not completed.");
            var result = new List<string>();
            string line;
            while (lines.TryTake(out line)) result.Add(line);
            return result.ToArray();
        }

        public int CharacterCount { get { return Volatile.Read(ref characterCount); } }

        public void Dispose()
        {
            try { reader.Dispose(); }
            catch { }
            if (completed) lines.Dispose();
        }
    }

    public sealed class BoundedTextDrain : IDisposable
    {
        private readonly StreamReader reader;
        private readonly int maximumCharacters;
        private readonly string label;
        private readonly Task pump;
        private volatile Exception fault;
        private int characterCount;

        public BoundedTextDrain(Stream stream, int maximumCharacters, string label)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (maximumCharacters <= 0) throw new ArgumentOutOfRangeException("maximumCharacters");
            this.maximumCharacters = maximumCharacters;
            this.label = label ?? "stream";
            reader = new StreamReader(stream, new UTF8Encoding(false, true), false, 4096, true);
            pump = Task.Run(async () => await PumpAsync().ConfigureAwait(false));
        }

        private async Task PumpAsync()
        {
            var buffer = new char[4096];
            try
            {
                while (true)
                {
                    var read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (read == 0) break;
                    if (Interlocked.Add(ref characterCount, read) > maximumCharacters)
                        throw new InvalidOperationException(label + " exceeded the maximum of " + maximumCharacters + " characters.");
                }
            }
            catch (Exception exception)
            {
                fault = exception;
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds) { return pump.Wait(timeoutMilliseconds); }

        public void ThrowIfFaulted()
        {
            if (fault != null) throw new InvalidOperationException(label + " reader failed: " + fault.Message, fault);
        }

        public int CharacterCount { get { return Volatile.Read(ref characterCount); } }

        public void Dispose()
        {
            try { reader.Dispose(); }
            catch { }
        }
    }
}
'@
}

function Get-SpherewrightStagedMcpProperty {
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Context
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { throw "$Context is missing '$Name'." }
    # Preserve scalar/object/array shape on both Windows PowerShell and pwsh.
    # Write-Output -NoEnumerate can wrap a scalar PSCustomObject in List<object>.
    return ,($property.Value)
}

function Get-SpherewrightExpectedProductVersionCore {
    param(
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string]$Context
    )

    $match = [regex]::Match($Version, '^(?<core>\d+\.\d+\.\d+)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')
    if (-not $match.Success) { throw "$Context must be a semantic product version with a three-part core: $Version" }
    return $match.Groups['core'].Value
}

function Get-SpherewrightServerProductVersionCore {
    param(
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string]$Context
    )

    try {
        $parsed = [version]$Version
    } catch {
        throw "$Context must be a numeric assembly version with a three-part product core: $Version"
    }
    if ($parsed.Major -lt 0 -or $parsed.Minor -lt 0 -or $parsed.Build -lt 0) {
        throw "$Context must expose a three-part product core: $Version"
    }
    return "$($parsed.Major).$($parsed.Minor).$($parsed.Build)"
}

function Get-SpherewrightStagedMcpTotalRemainingMilliseconds {
    param([Parameter(Mandatory)][Diagnostics.Stopwatch]$Stopwatch)

    $remaining = $script:SpherewrightStagedMcpTotalDeadlineMilliseconds - [int]$Stopwatch.ElapsedMilliseconds
    if ($remaining -le 0) { throw 'Staged MCP metadata probe exceeded its total deadline.' }
    return [Math]::Min($script:SpherewrightStagedMcpRequestTimeoutMilliseconds, $remaining)
}

function ConvertFrom-SpherewrightStagedMcpResponse {
    param(
        [Parameter(Mandatory)][string]$Line,
        [Parameter(Mandatory)][int]$Id,
        [Parameter(Mandatory)][string]$Method
    )

    try {
        $response = $Line | ConvertFrom-Json
    } catch {
        throw "Staged MCP emitted non-protocol stdout before the $Method response."
    }
    $jsonRpc = if ($null -eq $response) { $null } else { $response.PSObject.Properties['jsonrpc'] }
    $responseId = if ($null -eq $response) { $null } else { $response.PSObject.Properties['id'] }
    $result = if ($null -eq $response) { $null } else { $response.PSObject.Properties['result'] }
    $error = if ($null -eq $response) { $null } else { $response.PSObject.Properties['error'] }
    if ($null -eq $jsonRpc -or $null -eq $responseId -or $null -eq $result -or
        $jsonRpc.Value -cne '2.0' -or $responseId.Value -ne $Id -or $null -ne $error) {
        throw "Staged MCP returned an invalid protocol response for $Method."
    }
    return $response.result
}

function Invoke-SpherewrightStagedMcpMetadataRequest {
    param(
        [Parameter(Mandatory)][Diagnostics.Process]$Process,
        [Parameter(Mandatory)][object]$Stdout,
        [Parameter(Mandatory)][object]$Stderr,
        [Parameter(Mandatory)][Diagnostics.Stopwatch]$Stopwatch,
        [Parameter(Mandatory)][int]$Id,
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][object]$Parameters
    )

    $allowedMethods = @('initialize', 'tools/list', 'resources/list', 'resources/read')
    if ($allowedMethods -cnotcontains $Method) { throw "Staged MCP metadata probe forbids request method: $Method" }
    $Stderr.ThrowIfFaulted()
    $message = [ordered]@{ jsonrpc='2.0'; id=$Id; method=$Method; params=$Parameters } | ConvertTo-Json -Depth 16 -Compress
    $Process.StandardInput.WriteLine($message)
    $Process.StandardInput.Flush()
    $requestDeadline = [Math]::Min($script:SpherewrightStagedMcpTotalDeadlineMilliseconds, [int]$Stopwatch.ElapsedMilliseconds + $script:SpherewrightStagedMcpRequestTimeoutMilliseconds)
    $line = $null
    while ($true) {
        $remaining = $requestDeadline - [int]$Stopwatch.ElapsedMilliseconds
        if ($remaining -le 0) { throw "Staged MCP metadata request timed out: $Method" }
        try {
            $line = $Stdout.ReadLine([Math]::Min(250, $remaining))
            break
        } catch [TimeoutException] {
            $Stderr.ThrowIfFaulted()
        }
    }
    if ($null -eq $line) {
        $Process.WaitForExit(100) | Out-Null
        if ($Process.HasExited) { throw "Staged MCP exited with code $($Process.ExitCode) before the $Method response." }
        throw "Staged MCP closed stdout before the $Method response."
    }
    return ConvertFrom-SpherewrightStagedMcpResponse -Line $line -Id $Id -Method $Method
}

function Send-SpherewrightStagedMcpInitializedNotification {
    param(
        [Parameter(Mandatory)][Diagnostics.Process]$Process,
        [Parameter(Mandatory)][object]$Stderr,
        [Parameter(Mandatory)][Diagnostics.Stopwatch]$Stopwatch
    )

    $Stderr.ThrowIfFaulted()
    $null = Get-SpherewrightStagedMcpTotalRemainingMilliseconds $Stopwatch
    $notification = [ordered]@{ jsonrpc='2.0'; method='notifications/initialized' } | ConvertTo-Json -Compress
    $Process.StandardInput.WriteLine($notification)
    $Process.StandardInput.Flush()
}

function Invoke-SpherewrightStagedMcpProbe {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$ExecutablePath,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$ExpectedVersion,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$ExpectedPlaybookPath,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$IsolationDirectory
    )

    $resolvedExecutable = [IO.Path]::GetFullPath($ExecutablePath)
    if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) { throw "Staged MCP executable was not found: $resolvedExecutable" }
    $resolvedPlaybook = [IO.Path]::GetFullPath($ExpectedPlaybookPath)
    if (-not (Test-Path -LiteralPath $resolvedPlaybook -PathType Leaf)) { throw "Expected staged playbook was not found: $resolvedPlaybook" }
    $playbookItem = Get-Item -LiteralPath $resolvedPlaybook -Force
    if ($playbookItem.Length -gt $script:SpherewrightStagedMcpStdoutCharacterLimit) {
        throw "Expected staged playbook exceeds the bounded MCP stdout allowance: $resolvedPlaybook"
    }
    $expectedPlaybook = [IO.File]::ReadAllText($resolvedPlaybook)
    $expectedVersionCore = Get-SpherewrightExpectedProductVersionCore -Version $ExpectedVersion -Context 'ExpectedVersion'

    $resolvedIsolation = [IO.Path]::GetFullPath($IsolationDirectory)
    if (Test-Path -LiteralPath $resolvedIsolation) { throw "IsolationDirectory must not already exist: $resolvedIsolation" }
    $isolationParent = Split-Path -Parent $resolvedIsolation
    if ([string]::IsNullOrWhiteSpace($isolationParent) -or -not (Test-Path -LiteralPath $isolationParent -PathType Container)) {
        throw 'IsolationDirectory must be a new child of an existing caller-owned stage directory.'
    }
    $parentItem = Get-Item -LiteralPath $isolationParent -Force
    if (($parentItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -or -not $parentItem.PSIsContainer) {
        throw 'IsolationDirectory parent must be a non-reparse stage directory.'
    }
    [void][IO.Directory]::CreateDirectory($resolvedIsolation)
    $localAppData = Join-Path $resolvedIsolation 'localappdata'
    [void][IO.Directory]::CreateDirectory($localAppData)
    $missingDescriptor = Join-Path $resolvedIsolation 'bridge-descriptor-unavailable.json'
    if (Test-Path -LiteralPath $missingDescriptor) { throw 'The staged MCP bridge descriptor isolation path must remain nonexistent.' }

    Initialize-SpherewrightStagedMcpBoundedReaders
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $resolvedExecutable
    $start.WorkingDirectory = Split-Path -Parent $resolvedExecutable
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.EnvironmentVariables['SPHEREWRIGHT_BRIDGE_DESCRIPTOR'] = $missingDescriptor
    $start.EnvironmentVariables['LOCALAPPDATA'] = $localAppData

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $started = $false
    $stdout = $null
    $stderr = $null
    try {
        if (-not $process.Start()) { throw 'Staged MCP executable did not start.' }
        $started = $true
        $stdout = [Spherewright.StagedMcpProbe.BoundedLineReader]::new($process.StandardOutput.BaseStream, $script:SpherewrightStagedMcpStdoutCharacterLimit, 'Staged MCP stdout')
        $stderr = [Spherewright.StagedMcpProbe.BoundedTextDrain]::new($process.StandardError.BaseStream, $script:SpherewrightStagedMcpStderrCharacterLimit, 'Staged MCP stderr')
        $stopwatch = [Diagnostics.Stopwatch]::StartNew()

        $initialize = Invoke-SpherewrightStagedMcpMetadataRequest -Process $process -Stdout $stdout -Stderr $stderr -Stopwatch $stopwatch -Id 1 -Method 'initialize' -Parameters ([ordered]@{
            protocolVersion='2025-06-18'; capabilities=@{}; clientInfo=[ordered]@{name='spherewright-staged-metadata'; version='1.0.0'}
        })
        $serverInfo = Get-SpherewrightStagedMcpProperty -Object $initialize -Name 'serverInfo' -Context 'initialize result'
        $serverVersion = Get-SpherewrightStagedMcpProperty -Object $serverInfo -Name 'version' -Context 'initialize serverInfo'
        $actualVersionCore = Get-SpherewrightServerProductVersionCore -Version ([string]$serverVersion) -Context 'Staged MCP server version'
        if (-not [string]::Equals($actualVersionCore, $expectedVersionCore, [StringComparison]::Ordinal)) {
            throw "Staged MCP product version core $actualVersionCore does not match expected $expectedVersionCore."
        }

        Send-SpherewrightStagedMcpInitializedNotification -Process $process -Stderr $stderr -Stopwatch $stopwatch
        $toolsResult = Invoke-SpherewrightStagedMcpMetadataRequest -Process $process -Stdout $stdout -Stderr $stderr -Stopwatch $stopwatch -Id 2 -Method 'tools/list' -Parameters @{}
        $toolsValue = Get-SpherewrightStagedMcpProperty -Object $toolsResult -Name 'tools' -Context 'tools/list result'
        if ($null -eq $toolsValue) { throw 'Staged MCP tools/list returned no tools collection.' }
        $tools = @($toolsValue)
        if ($tools.Count -eq 0) { throw 'Staged MCP tools/list returned an empty tools collection.' }
        $toolNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($tool in $tools) {
            $name = Get-SpherewrightStagedMcpProperty -Object $tool -Name 'name' -Context 'tools/list tool'
            if ($name -isnot [string] -or [string]::IsNullOrWhiteSpace($name) -or -not $toolNames.Add($name)) {
                throw 'Staged MCP tools/list contains an empty or duplicate tool name.'
            }
        }
        foreach ($requiredTool in @('spherewright_get_status', 'spherewright_get_session_state')) {
            if (-not $toolNames.Contains($requiredTool)) { throw "Staged MCP tools/list is missing required tool $requiredTool." }
        }

        $resourcesResult = Invoke-SpherewrightStagedMcpMetadataRequest -Process $process -Stdout $stdout -Stderr $stderr -Stopwatch $stopwatch -Id 3 -Method 'resources/list' -Parameters @{}
        $resourcesValue = Get-SpherewrightStagedMcpProperty -Object $resourcesResult -Name 'resources' -Context 'resources/list result'
        if ($null -eq $resourcesValue) { throw 'Staged MCP resources/list returned no resources collection.' }
        $resources = @($resourcesValue)
        $resourceUris = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $openingMovementMatches = 0
        foreach ($resource in $resources) {
            $uri = Get-SpherewrightStagedMcpProperty -Object $resource -Name 'uri' -Context 'resources/list resource'
            if ($uri -isnot [string] -or [string]::IsNullOrWhiteSpace($uri) -or -not $resourceUris.Add($uri)) {
                throw 'Staged MCP resources/list contains an empty or duplicate resource URI.'
            }
            if ([string]::Equals([string]$uri, $script:SpherewrightStagedMcpOpeningMovementUri, [StringComparison]::Ordinal)) { $openingMovementMatches++ }
        }
        if ($openingMovementMatches -ne 1) { throw 'Staged MCP must advertise exactly one opening-movement playbook resource.' }

        $readResult = Invoke-SpherewrightStagedMcpMetadataRequest -Process $process -Stdout $stdout -Stderr $stderr -Stopwatch $stopwatch -Id 4 -Method 'resources/read' -Parameters @{uri=$script:SpherewrightStagedMcpOpeningMovementUri}
        $contentsValue = Get-SpherewrightStagedMcpProperty -Object $readResult -Name 'contents' -Context 'resources/read result'
        if ($null -eq $contentsValue) { throw 'Staged MCP resources/read returned no contents collection.' }
        $contents = @($contentsValue)
        if ($contents.Count -ne 1) { throw 'Staged MCP resources/read must return exactly one opening-movement content item.' }
        $contentUri = Get-SpherewrightStagedMcpProperty -Object $contents[0] -Name 'uri' -Context 'resources/read content'
        if ($contentUri -isnot [string] -or -not [string]::Equals($contentUri, $script:SpherewrightStagedMcpOpeningMovementUri, [StringComparison]::Ordinal)) {
            throw 'Staged MCP resources/read returned an unexpected playbook URI.'
        }
        $resourcePlaybook = Get-SpherewrightStagedMcpProperty -Object $contents[0] -Name 'text' -Context 'resources/read content'
        if ($resourcePlaybook -isnot [string] -or -not [string]::Equals($resourcePlaybook, $expectedPlaybook, [StringComparison]::Ordinal)) {
            throw 'The staged MCP opening-movement playbook does not exactly match the staged playbook file.'
        }

        $process.StandardInput.Close()
        $remaining = Get-SpherewrightStagedMcpTotalRemainingMilliseconds $stopwatch
        if (-not $process.WaitForExit($remaining)) { throw 'Staged MCP did not exit after stdin closed before the total deadline.' }
        if ($process.ExitCode -ne 0) { throw "Staged MCP exited with code $($process.ExitCode) after stdin closed." }
        $remaining = Get-SpherewrightStagedMcpTotalRemainingMilliseconds $stopwatch
        if (-not $stdout.WaitForCompletion($remaining)) { throw 'Staged MCP stdout did not finish draining before the total deadline.' }
        $stdout.ThrowIfFaulted()
        $remaining = Get-SpherewrightStagedMcpTotalRemainingMilliseconds $stopwatch
        if (-not $stderr.WaitForCompletion($remaining)) { throw 'Staged MCP stderr did not finish draining before the total deadline.' }
        $stderr.ThrowIfFaulted()
        $extraOutput = @($stdout.DrainLines())
        if ($extraOutput.Count -ne 0) { throw 'Staged MCP emitted extra stdout after the final protocol response.' }

        return [pscustomobject][ordered]@{
            version = [string]$serverVersion
            tools = $tools.Count
            resources = $resources.Count
            playbookMatches = $true
            extraStdoutCharacters = 0
            exitCode = $process.ExitCode
        }
    }
    finally {
        # This Process object is created only for this probe; never target DSP or a pre-existing process.
        if ($started -and -not $process.HasExited) {
            try { $process.StandardInput.Close() } catch { }
            if (-not $process.WaitForExit(500)) {
                try { $process.Kill() } catch { }
                try { $process.WaitForExit(2000) | Out-Null } catch { }
            }
        }
        if ($null -ne $stdout) {
            try { $stdout.WaitForCompletion(2000) | Out-Null } catch { }
            $stdout.Dispose()
        }
        if ($null -ne $stderr) {
            try { $stderr.WaitForCompletion(2000) | Out-Null } catch { }
            $stderr.Dispose()
        }
        $process.Dispose()
    }
}
