[CmdletBinding()]
param(
    [string]$SourceMcpExecutable
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
. (Join-Path $PSScriptRoot 'Test-SpherewrightStagedMcp.ps1')
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('spherewright-staged-mcp-tests-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testRoot)
$testCases = 0

function New-SpherewrightStagedMcpFixtureExecutable {
    param([Parameter(Mandatory)][string]$Destination)

    $source = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

public static class SpherewrightStagedMcpFixture
{
    private const string OpeningMovementUri = "spherewright://agent/playbooks/opening-movement-v1";

    public static int Main()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (String.IsNullOrWhiteSpace(localAppData)) return 31;
        var localItem = new DirectoryInfo(localAppData);
        if (localItem.Parent == null || !String.Equals(localItem.Name, "localappdata", StringComparison.Ordinal)) return 32;
        var isolation = localItem.Parent;
        var descriptor = Environment.GetEnvironmentVariable("SPHEREWRIGHT_BRIDGE_DESCRIPTOR");
        var expectedDescriptor = Path.Combine(isolation.FullName, "bridge-descriptor-unavailable.json");
        if (!String.Equals(descriptor, expectedDescriptor, StringComparison.Ordinal) || File.Exists(expectedDescriptor)) return 33;

        var calls = new List<string>();
        try
        {
            var mode = isolation.Name;
            if (HasMode(mode, "exit-nonzero"))
            {
                Console.In.ReadLine();
                return 7;
            }

            string line;
            while ((line = Console.In.ReadLine()) != null)
            {
                var method = GetMethod(line);
                calls.Add(method ?? "<unknown>");
                if (HasMode(mode, "timeout"))
                {
                    Thread.Sleep(30000);
                    continue;
                }
                if (HasMode(mode, "nonprotocol"))
                {
                    Console.WriteLine("not-jsonrpc");
                    return 0;
                }
                if (HasMode(mode, "stdout-overlimit"))
                {
                    Console.Out.Write(new string('x', 600000));
                    Console.Out.Flush();
                    Thread.Sleep(30000);
                    continue;
                }
                if (HasMode(mode, "stderr-overlimit"))
                {
                    Console.Error.Write(new string('e', 150000));
                    Console.Error.Flush();
                    Thread.Sleep(30000);
                    continue;
                }
                if (String.Equals(method, "notifications/initialized", StringComparison.Ordinal)) continue;
                var id = GetId(line);
                if (String.Equals(method, "initialize", StringComparison.Ordinal))
                {
                    var version = HasMode(mode, "version-mismatch") ? "9.9.9" : "1.2.3";
                    Reply(id, "{\"protocolVersion\":\"2025-06-18\",\"serverInfo\":{\"name\":\"fixture\",\"version\":\"" + version + "\"},\"capabilities\":{}}");
                }
                else if (String.Equals(method, "tools/list", StringComparison.Ordinal))
                {
                    var tools = HasMode(mode, "missing-tool")
                        ? "[{\"name\":\"spherewright_get_status\"}]"
                        : "[{\"name\":\"spherewright_get_status\"},{\"name\":\"spherewright_get_session_state\"}]";
                    Reply(id, "{\"tools\":" + tools + "}");
                }
                else if (String.Equals(method, "resources/list", StringComparison.Ordinal))
                {
                    var resources = HasMode(mode, "missing-resource")
                        ? "[]"
                        : "[{\"uri\":\"" + OpeningMovementUri + "\"}]";
                    Reply(id, "{\"resources\":" + resources + "}");
                }
                else if (String.Equals(method, "resources/read", StringComparison.Ordinal))
                {
                    if (line.IndexOf(OpeningMovementUri, StringComparison.Ordinal) < 0) return 34;
                    var text = HasMode(mode, "guide-mismatch") ? "fixture-playbook-mismatch" : "fixture-playbook";
                    Reply(id, "{\"contents\":[{\"uri\":\"" + OpeningMovementUri + "\",\"mimeType\":\"text/markdown\",\"text\":\"" + text + "\"}]}");
                }
                else
                {
                    return 35;
                }
            }
            return 0;
        }
        finally
        {
            File.WriteAllLines(Path.Combine(isolation.FullName, "fixture-requests.log"), calls.ToArray());
        }
    }

    private static bool HasMode(string mode, string expected)
    {
        return mode.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetMethod(string line)
    {
        var match = Regex.Match(line, "\\\"method\\\"\\s*:\\s*\\\"(?<value>[^\\\"]+)\\\"");
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static string GetId(string line)
    {
        var match = Regex.Match(line, "\\\"id\\\"\\s*:\\s*(?<value>[0-9]+)");
        return match.Success ? match.Groups["value"].Value : "0";
    }

    private static void Reply(string id, string result)
    {
        Console.WriteLine("{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + result + "}");
        Console.Out.Flush();
    }
}
'@
    $sourcePath = [IO.Path]::ChangeExtension($Destination, '.cs')
    [IO.File]::WriteAllText($sourcePath, $source)
    $compilerCandidates = @(
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
    )
    $compiler = @($compilerCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1)[0]
    if ([string]::IsNullOrWhiteSpace($compiler)) { throw 'A Windows .NET Framework C# compiler is required for the temporary staged MCP fixture.' }
    & $compiler /nologo /target:exe ("/out:$Destination") $sourcePath
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic staged MCP fixture compilation failed.' }
    if (-not (Test-Path -LiteralPath $Destination -PathType Leaf)) { throw 'Synthetic staged MCP fixture executable was not produced.' }
    return [IO.Path]::GetFullPath($Destination)
}

function Assert-SpherewrightStagedMcpFailure {
    param(
        [Parameter(Mandatory)][string]$Mode,
        [Parameter(Mandatory)][string]$ExpectedMessage,
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string]$Playbook,
        [Parameter(Mandatory)][string]$Stage
    )

    $isolation = Join-Path $Stage ($Mode + '-' + [guid]::NewGuid().ToString('N'))
    $failed = $false
    try {
        Invoke-SpherewrightStagedMcpProbe -ExecutablePath $Executable -ExpectedVersion '1.2.3' -ExpectedPlaybookPath $Playbook -IsolationDirectory $isolation | Out-Null
    } catch {
        if ($_.Exception.Message -notlike "*$ExpectedMessage*") { throw }
        $failed = $true
    }
    if (-not $failed) { throw "Expected staged MCP probe failure for $Mode." }
    $script:testCases++
}

try {
    $stage = Join-Path $testRoot 'caller-stage'
    [void][IO.Directory]::CreateDirectory($stage)
    $fixturePlaybook = Join-Path $stage 'fixture-playbook.md'
    [IO.File]::WriteAllText($fixturePlaybook, 'fixture-playbook')
    $fixtureExecutable = New-SpherewrightStagedMcpFixtureExecutable (Join-Path $testRoot 'SpherewrightStagedMcpFixture.exe')
    $parentDescriptorBefore = $env:SPHEREWRIGHT_BRIDGE_DESCRIPTOR
    $parentLocalAppDataBefore = $env:LOCALAPPDATA

    $successIsolation = Join-Path $stage ('success-' + [guid]::NewGuid().ToString('N'))
    $success = Invoke-SpherewrightStagedMcpProbe -ExecutablePath $fixtureExecutable -ExpectedVersion '1.2.3' -ExpectedPlaybookPath $fixturePlaybook -IsolationDirectory $successIsolation
    if ($success.version -cne '1.2.3' -or $success.tools -ne 2 -or $success.resources -ne 1 -or -not $success.playbookMatches -or $success.extraStdoutCharacters -ne 0 -or $success.exitCode -ne 0) {
        throw 'Synthetic staged MCP success metadata was incomplete.'
    }
    $requests = @(Get-Content -LiteralPath (Join-Path $successIsolation 'fixture-requests.log') | ForEach-Object { [string]$_ })
    $expectedRequests = @('initialize', 'notifications/initialized', 'tools/list', 'resources/list', 'resources/read')
    if ([string]::Join("`n", $requests) -cne [string]::Join("`n", $expectedRequests)) {
        throw 'Staged MCP probe sent requests outside its closed metadata sequence.'
    }
    if (Test-Path -LiteralPath (Join-Path $successIsolation 'bridge-descriptor-unavailable.json')) {
        throw 'Staged MCP probe created its intentionally nonexistent bridge descriptor path.'
    }
    $testCases++

    Assert-SpherewrightStagedMcpFailure -Mode 'version-mismatch' -ExpectedMessage 'product version core' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'missing-tool' -ExpectedMessage 'missing required tool' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'missing-resource' -ExpectedMessage 'exactly one opening-movement' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'guide-mismatch' -ExpectedMessage 'does not exactly match' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'nonprotocol' -ExpectedMessage 'non-protocol stdout' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'timeout' -ExpectedMessage 'metadata request timed out' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'exit-nonzero' -ExpectedMessage 'exited with code 7' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'stdout-overlimit' -ExpectedMessage 'exceeded the maximum' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage
    Assert-SpherewrightStagedMcpFailure -Mode 'stderr-overlimit' -ExpectedMessage 'exceeded the maximum' -Executable $fixtureExecutable -Playbook $fixturePlaybook -Stage $stage

    $existingIsolation = Join-Path $stage 'already-exists'
    [void][IO.Directory]::CreateDirectory($existingIsolation)
    $existingRejected = $false
    try {
        Invoke-SpherewrightStagedMcpProbe -ExecutablePath $fixtureExecutable -ExpectedVersion '1.2.3' -ExpectedPlaybookPath $fixturePlaybook -IsolationDirectory $existingIsolation | Out-Null
    } catch {
        if ($_.Exception.Message -notlike '*must not already exist*') { throw }
        $existingRejected = $true
    }
    if (-not $existingRejected) { throw 'Staged MCP probe accepted an existing isolation directory.' }
    $testCases++

    if ([string]::IsNullOrWhiteSpace($SourceMcpExecutable)) {
        $candidates = @(
            (Join-Path $repoRoot 'src/Spherewright.Mcp/bin/Release/net8.0/Spherewright.Mcp.exe'),
            (Join-Path $repoRoot 'src/Spherewright.Mcp/bin/Release/net8.0/win-x64/Spherewright.Mcp.exe')
        )
        $SourceMcpExecutable = @($candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1)[0]
    }
    if ([string]::IsNullOrWhiteSpace($SourceMcpExecutable)) { throw 'Build the current Release MCP executable before running this test.' }
    [xml]$buildProps = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props') -Raw
    $sourceVersion = [string]$buildProps.Project.PropertyGroup.VersionPrefix
    $sourceIsolation = Join-Path $stage ('source-metadata-' + [guid]::NewGuid().ToString('N'))
    $sourceProbe = Invoke-SpherewrightStagedMcpProbe -ExecutablePath $SourceMcpExecutable -ExpectedVersion $sourceVersion -ExpectedPlaybookPath (Join-Path $repoRoot 'docs/agent-playbook.md') -IsolationDirectory $sourceIsolation
    if ($sourceProbe.tools -lt 1 -or $sourceProbe.resources -lt 1 -or -not $sourceProbe.playbookMatches -or $sourceProbe.extraStdoutCharacters -ne 0 -or $sourceProbe.exitCode -ne 0) {
        throw 'Current source-built MCP did not pass its metadata-only staged probe.'
    }
    $testCases++

    if ($env:SPHEREWRIGHT_BRIDGE_DESCRIPTOR -cne $parentDescriptorBefore -or $env:LOCALAPPDATA -cne $parentLocalAppDataBefore) {
        throw 'Staged MCP probe modified the parent process environment.'
    }
    $testCases++
    [pscustomobject]@{
        passed = $testCases
        scope = 'temporary synthetic child processes plus one source-built metadata-only MCP probe; no Bridge or gameplay requests'
        sourceTools = $sourceProbe.tools
        sourceResources = $sourceProbe.resources
        sourcePlaybookCharacters = [IO.File]::ReadAllText((Join-Path $repoRoot 'docs/agent-playbook.md')).Length
    } | ConvertTo-Json -Depth 3
}
finally {
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $leaf = Split-Path -Leaf $resolvedTestRoot
    if ($resolvedTestRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [regex]::IsMatch($leaf, '^spherewright-staged-mcp-tests-[0-9a-f]{32}$', [Text.RegularExpressions.RegexOptions]::IgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTestRoot)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
