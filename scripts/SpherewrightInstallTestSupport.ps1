# Shared synthetic filesystem fixture for the transaction and explicit-recovery
# script tests. This is deliberately test-only and has no installer entrypoint.

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
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][object]$ExpectedFiles,
        [string[]]$ExcludeRelativePaths = @()
    )
    $lines = [Collections.Generic.List[string]]::new()
    $rootItem = Get-Item -LiteralPath $Root -Force -ErrorAction SilentlyContinue
    $lines.Add('root=' + ($null -ne $rootItem))
    foreach ($relative in @($ExpectedFiles.Keys | Sort-Object)) {
        if ($ExcludeRelativePaths -contains $relative) { continue }
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
        [switch]$SkipPluginPayload,
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
    if (-not $SkipPluginPayload) {
        [void][IO.Directory]::CreateDirectory($pluginDestination)
        Write-TestInstallTransactionFile -Path (Join-Path $pluginDestination 'Spherewright.Plugin.dll') -Content 'old plugin main'
        Write-TestInstallTransactionFile -Path (Join-Path $pluginDestination 'Spherewright.Contracts.dll') -Content 'old contracts'
        Write-TestInstallTransactionFile -Path (Join-Path $pluginDestination 'runtime-handoff\handoff.json') -Content 'handoff must remain unchanged'
    }
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

function Assert-TestInstallTransactionOldDependenciesAndMcpRestoredWithMainWithheld {
    param(
        [Parameter(Mandatory)][object]$Fixture,
        [Parameter(Mandatory)][object]$Pair,
        [Parameter(Mandatory)][string]$PluginBefore,
        [Parameter(Mandatory)][string]$McpBefore,
        [Parameter(Mandatory)][string]$HandoffBefore,
        [string]$Context = ''
    )
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionSnapshot -Root $Fixture.pluginDestination -ExpectedFiles $Pair.pluginExpected -ExcludeRelativePaths @('Spherewright.Plugin.dll')) -ceq $PluginBefore) -Message "Unresolved failure did not restore the old non-main Plugin payload set: $($Fixture.root) $Context"
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionSnapshot -Root $Fixture.mcpDestination -ExpectedFiles $Pair.mcpExpected) -ceq $McpBefore) -Message "Unresolved failure did not restore the old MCP payload set: $($Fixture.root) $Context"
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root (Join-Path $Fixture.pluginDestination 'runtime-handoff')) -ceq $HandoffBefore) -Message 'Unresolved failure changed runtime-handoff.'
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Join-Path $Fixture.pluginDestination 'Spherewright.Plugin.dll') -PathType Leaf)) -Message "Unresolved failure left a live main Plugin DLL: $($Fixture.root) $Context"
}

function Assert-TestInstallTransactionPromotedDependenciesAndMcpWithMainWithheld {
    param(
        [Parameter(Mandatory)][object]$Fixture,
        [Parameter(Mandatory)][object]$Pair,
        [Parameter(Mandatory)][string]$HandoffBefore,
        [string]$Context = ''
    )
    $expectedPluginLines = [Collections.Generic.List[string]]::new()
    $expectedPluginLines.Add('root=True')
    foreach ($relative in @($Pair.pluginExpected.Keys | Sort-Object)) {
        if ($relative -cne 'Spherewright.Plugin.dll') { $expectedPluginLines.Add("$relative=$($Pair.pluginExpected[$relative])") }
    }
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionSnapshot -Root $Fixture.pluginDestination -ExpectedFiles $Pair.pluginExpected -ExcludeRelativePaths @('Spherewright.Plugin.dll')) -ceq ($expectedPluginLines -join "`n")) -Message "Pending-marker delete failure changed promoted non-main Plugin payloads: $($Fixture.root) $Context"
    $null = Assert-SpherewrightInstallSnapshotComplete -Root $Fixture.mcpDestination -ExpectedFiles $Pair.mcpExpected
    Assert-TestInstallTransaction -Condition ((Get-TestInstallTransactionTree -Root (Join-Path $Fixture.pluginDestination 'runtime-handoff')) -ceq $HandoffBefore) -Message "Pending-marker delete failure changed runtime-handoff: $($Fixture.root) $Context"
    Assert-TestInstallTransaction -Condition (-not (Test-Path -LiteralPath (Join-Path $Fixture.pluginDestination 'Spherewright.Plugin.dll') -PathType Leaf)) -Message "Pending-marker delete failure left the promoted main Plugin DLL live: $($Fixture.root) $Context"
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
