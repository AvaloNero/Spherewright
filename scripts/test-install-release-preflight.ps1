[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Synthetic package/installation only. Never discover or operate a real game.
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('spherewright-install-tests-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testRoot)
$installer = Join-Path $PSScriptRoot 'install-release.ps1'
$testCases = 0
function Get-Process { [CmdletBinding()] param([string]$Name) if ($Name -cne 'DSPGAME') { throw 'Unexpected process query' } }
$global:SpherewrightSyntheticCopyFaultAfter = 0
$global:SpherewrightSyntheticCopyFaultCount = 0
function Copy-Item {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string[]]$LiteralPath,
        [Parameter(Mandatory)][string]$Destination,
        [switch]$Force,
        [switch]$Recurse,
        [switch]$PassThru,
        [switch]$Container
    )
    if ($global:SpherewrightSyntheticCopyFaultAfter -gt 0) {
        $global:SpherewrightSyntheticCopyFaultCount++
        if ($global:SpherewrightSyntheticCopyFaultCount -ge $global:SpherewrightSyntheticCopyFaultAfter) {
            throw 'Synthetic staging copy failure.'
        }
    }
    Microsoft.PowerShell.Management\Copy-Item @PSBoundParameters
}
function New-Fixture {
    $root = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
    $package = Join-Path $root 'package'
    $game = Join-Path $root 'game'
    $mcp = Join-Path $root 'installed-mcp'
    $runtime = Join-Path $root 'protected-runtime'
    $handoff = "$game/BepInEx/plugins/Spherewright/runtime-handoff"
    foreach ($dir in @($package, "$package/BepInEx/plugins/Spherewright", "$package/mcp", "$game/BepInEx/core", "$game/BepInEx/plugins/Spherewright", $mcp, $runtime, $handoff)) { [void][IO.Directory]::CreateDirectory($dir) }
    Copy-Item -LiteralPath $installer -Destination "$package/install.ps1"
    [IO.File]::WriteAllText("$package/locate-dsp.ps1", 'param([string]$DspDir,[switch]$AsJson) @{path=$DspDir;source="synthetic"} | ConvertTo-Json')
    foreach ($name in @('Spherewright.Plugin.dll','Spherewright.Contracts.dll','Spherewright.Bridge.Core.dll','Newtonsoft.Json.dll')) { [IO.File]::WriteAllText("$package/BepInEx/plugins/Spherewright/$name", "new-$name") }
    [IO.File]::WriteAllText("$package/mcp/Spherewright.Mcp.exe", 'synthetic-not-executable')
    [IO.File]::WriteAllText("$game/BepInEx/core/BepInEx.dll", 'synthetic')
    [IO.File]::WriteAllText("$game/BepInEx/plugins/Spherewright/Spherewright.Plugin.dll", 'old-plugin')
    [IO.File]::WriteAllText("$mcp/Spherewright.Mcp.exe", 'old-mcp')
    [IO.File]::WriteAllText("$runtime/descriptor.json", 'protected-runtime')
    [IO.File]::WriteAllText("$handoff/owned-world-resume.json", 'protected-handoff')
    return @{root=$root;package=$package;game=$game;mcp=$mcp;runtime=$runtime;handoff=$handoff}
}
function Write-Manifest($fixture) {
    $files = @(Get-ChildItem -LiteralPath $fixture.package -File -Recurse -Force | Where-Object Name -ne 'manifest.json' | ForEach-Object {
        @{path=$_.FullName.Substring($fixture.package.Length+1).Replace('\','/');size=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}
    })
    $manifest = @{schemaVersion=1;package='Spherewright';version='0.4.0';files=$files}
    Save-Manifest $fixture $manifest
    return $manifest
}
function Save-Manifest($fixture,$manifest) { [IO.File]::WriteAllText((Join-Path $fixture.package 'manifest.json'), ($manifest | ConvertTo-Json -Depth 8)) }
function Get-TreeSnapshot([string]$root) {
    $rootItem=Get-Item -LiteralPath $root -Force -ErrorAction SilentlyContinue
    if ($null -eq $rootItem) { return '<missing>' }
    $entries=[Collections.Generic.List[string]]::new()
    $directories=[Collections.Generic.Stack[object]]::new()
    $directories.Push([pscustomobject]@{path=$rootItem.FullName;relative=''})
    while ($directories.Count -gt 0) {
        $current=$directories.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $current.path -Force)) {
            $relative=if ([string]::IsNullOrEmpty([string]$current.relative)) { $item.Name } else { "$($current.relative)/$($item.Name)" }
            $attributes=[int]$item.Attributes
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { $entries.Add("L|$relative|$attributes"); continue }
            if ($item.PSIsContainer) { $entries.Add("D|$relative|$attributes"); $directories.Push([pscustomobject]@{path=$item.FullName;relative=$relative}); continue }
            $entries.Add("F|$relative|$attributes|$((Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash)")
        }
    }
    return (@($entries | Sort-Object) -join "`n")
}
function Get-StageSnapshot([string]$parent) {
    $item=Get-Item -LiteralPath $parent -Force -ErrorAction SilentlyContinue
    if ($null -eq $item) { return '<missing-parent>' }
    $entries=@(Get-ChildItem -LiteralPath $item.FullName -Force | Where-Object { $_.Name -like '.spherewright-stage-*' } | ForEach-Object {
        "{0}|{1}" -f $_.FullName,(Get-TreeSnapshot $_.FullName)
    } | Sort-Object)
    return ($entries -join "`n---`n")
}
function Assert-ExactInstalledSet($fixture,$manifest) {
    $pluginRoot="$($fixture.game)/BepInEx/plugins/Spherewright"
    $pluginExpected=@('Spherewright.Plugin.dll','Spherewright.Contracts.dll','Spherewright.Bridge.Core.dll','Newtonsoft.Json.dll')
    $pluginActual=@(Get-ChildItem -LiteralPath $pluginRoot -File -Recurse -Force | ForEach-Object { $_.FullName.Substring($pluginRoot.Length+1).Replace('\','/') } | Where-Object { -not $_.StartsWith('runtime-handoff/', [StringComparison]::OrdinalIgnoreCase) } | Sort-Object)
    if ((ConvertTo-Json @($pluginActual)) -cne (ConvertTo-Json @($pluginExpected | Sort-Object))) { throw 'Plugin target does not contain the exact approved file set.' }
    foreach ($name in $pluginExpected) {
        $entry=@($manifest.files | Where-Object { $_.path -ceq "BepInEx/plugins/Spherewright/$name" })
        if ($entry.Count -ne 1 -or (Get-FileHash -LiteralPath "$pluginRoot/$name" -Algorithm SHA256).Hash -cne [string]$entry[0].sha256) { throw 'Plugin target hash mismatch.' }
    }
    $mcpExpected=@($manifest.files | Where-Object { ([string]$_.path).StartsWith('mcp/', [StringComparison]::OrdinalIgnoreCase) } | ForEach-Object { ([string]$_.path).Substring(4) } | Sort-Object)
    $mcpActual=@(Get-ChildItem -LiteralPath $fixture.mcp -File -Recurse -Force | ForEach-Object { $_.FullName.Substring($fixture.mcp.Length+1).Replace('\','/') } | Sort-Object)
    if ((ConvertTo-Json @($mcpActual)) -cne (ConvertTo-Json @($mcpExpected))) { throw 'MCP target does not contain the exact approved file set.' }
    foreach ($relative in $mcpExpected) {
        $entry=@($manifest.files | Where-Object { $_.path -ceq "mcp/$relative" })
        if ($entry.Count -ne 1 -or (Get-FileHash -LiteralPath "$($fixture.mcp)/$relative" -Algorithm SHA256).Hash -cne [string]$entry[0].sha256) { throw 'MCP target hash mismatch.' }
    }
}
function Assert-ExactStagedSet($fixture,$manifest,$stageResult) {
    $pluginRoot=[string]$stageResult.pluginStagedTo
    $mcpRoot=[string]$stageResult.mcpStagedTo
    $pluginExpected=@('Spherewright.Plugin.dll','Spherewright.Contracts.dll','Spherewright.Bridge.Core.dll','Newtonsoft.Json.dll') | Sort-Object
    $pluginActual=@(Get-ChildItem -LiteralPath $pluginRoot -File -Recurse -Force | ForEach-Object { $_.FullName.Substring($pluginRoot.Length+1).Replace('\','/') } | Sort-Object)
    if ((ConvertTo-Json @($pluginActual)) -cne (ConvertTo-Json @($pluginExpected))) { throw 'Plugin staging does not contain the exact approved file set.' }
    foreach ($name in $pluginExpected) {
        $entry=@($manifest.files | Where-Object { $_.path -ceq "BepInEx/plugins/Spherewright/$name" })
        if ($entry.Count -ne 1 -or (Get-FileHash -LiteralPath "$pluginRoot/$name" -Algorithm SHA256).Hash -cne [string]$entry[0].sha256) { throw 'Plugin staging hash mismatch.' }
    }
    $mcpExpected=@($manifest.files | Where-Object { ([string]$_.path).StartsWith('mcp/', [StringComparison]::OrdinalIgnoreCase) } | ForEach-Object { ([string]$_.path).Substring(4) } | Sort-Object)
    $mcpActual=@(Get-ChildItem -LiteralPath $mcpRoot -File -Recurse -Force | ForEach-Object { $_.FullName.Substring($mcpRoot.Length+1).Replace('\','/') } | Sort-Object)
    if ((ConvertTo-Json @($mcpActual)) -cne (ConvertTo-Json @($mcpExpected))) { throw 'MCP staging does not contain the exact approved file set.' }
    foreach ($relative in $mcpExpected) {
        $entry=@($manifest.files | Where-Object { $_.path -ceq "mcp/$relative" })
        if ($entry.Count -ne 1 -or (Get-FileHash -LiteralPath (Join-Path $mcpRoot $relative) -Algorithm SHA256).Hash -cne [string]$entry[0].sha256) { throw 'MCP staging hash mismatch.' }
    }
}
function New-TestJunction([string]$path,[string]$target) {
    New-Item -ItemType Junction -Path $path -Target $target -ErrorAction Stop | Out-Null
}
function Set-PackageAtPluginPath($fixture,[string]$relativePath) {
    $pluginRoot="$($fixture.game)/BepInEx/plugins/Spherewright"
    $target=if ([string]::IsNullOrEmpty($relativePath)) { $pluginRoot } else { Join-Path $pluginRoot $relativePath }
    [void][IO.Directory]::CreateDirectory($target)
    foreach ($item in @(Get-ChildItem -LiteralPath $fixture.package -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $target -Recurse -Force
    }
    $fixture.package=[IO.Path]::GetFullPath($target)
    return $fixture
}
function Reject-Install($fixture,[string]$message,[string]$destination=$fixture.mcp,[string]$game=$fixture.game) {
    $pluginBefore=Get-TreeSnapshot "$game/BepInEx/plugins/Spherewright"
    $mcpBefore=Get-TreeSnapshot $destination
    $runtimeBefore=Get-TreeSnapshot $fixture.runtime
    $handoffBefore=Get-TreeSnapshot $fixture.handoff
    $rejected=$false
    try { & "$($fixture.package)/install.ps1" -DspDir $game -McpDestination $destination -Force | Out-Null }
    catch { if (-not $_.Exception.Message.Contains($message)) { throw }; $rejected=$true }
    if (-not $rejected) { throw "Expected rejection: $message" }
    if ((Get-TreeSnapshot "$game/BepInEx/plugins/Spherewright") -cne $pluginBefore -or (Get-TreeSnapshot $destination) -cne $mcpBefore -or (Get-TreeSnapshot $fixture.runtime) -cne $runtimeBefore -or (Get-TreeSnapshot $fixture.handoff) -cne $handoffBefore) { throw 'Rejected/preflight install modified a destination or protected state.' }
    $script:testCases++
}
function Reject-Stage($fixture,[string]$message,[string]$destination=$fixture.mcp,[string]$game=$fixture.game,[switch]$AlsoPreflight) {
    $pluginBefore=Get-TreeSnapshot "$game/BepInEx/plugins/Spherewright"
    $mcpBefore=Get-TreeSnapshot $destination
    $runtimeBefore=Get-TreeSnapshot $fixture.runtime
    $handoffBefore=Get-TreeSnapshot $fixture.handoff
    $pluginStageBefore=Get-StageSnapshot "$game/BepInEx"
    $mcpParent=[IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($destination))
    $mcpStageBefore=Get-StageSnapshot $mcpParent
    $rejected=$false
    try {
        $stageArgs=@{DspDir=$game;McpDestination=$destination;Force=$true;StageOnly=$true}
        if ($AlsoPreflight) { $stageArgs.PreflightOnly=$true }
        & "$($fixture.package)/install.ps1" @stageArgs | Out-Null
    } catch { if (-not $_.Exception.Message.Contains($message)) { throw }; $rejected=$true }
    if (-not $rejected) { throw "Expected stage rejection: $message" }
    if ((Get-TreeSnapshot "$game/BepInEx/plugins/Spherewright") -cne $pluginBefore -or (Get-TreeSnapshot $destination) -cne $mcpBefore -or (Get-TreeSnapshot $fixture.runtime) -cne $runtimeBefore -or (Get-TreeSnapshot $fixture.handoff) -cne $handoffBefore -or (Get-StageSnapshot "$game/BepInEx") -cne $pluginStageBefore -or (Get-StageSnapshot $mcpParent) -cne $mcpStageBefore) { throw 'Rejected stage operation modified live, protected, or staging state.' }
    $script:testCases++
}
try {
    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    $pluginBefore=Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright"
    $mcpBefore=Get-TreeSnapshot $fixture.mcp
    $runtimeBefore=Get-TreeSnapshot $fixture.runtime
    $handoffBefore=Get-TreeSnapshot $fixture.handoff
    $pluginStageBefore=Get-StageSnapshot "$($fixture.game)/BepInEx"
    $mcpStageBefore=Get-StageSnapshot ([IO.Path]::GetDirectoryName($fixture.mcp))
    $result=& "$($fixture.package)/install.ps1" -DspDir $fixture.game -McpDestination $fixture.mcp -Force -PreflightOnly | ConvertFrom-Json
    if (-not $result.preflightOnly -or $result.installed -or -not $result.exactFileSet) { throw 'Invalid preview result' }
    if ((Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright") -cne $pluginBefore -or (Get-TreeSnapshot $fixture.mcp) -cne $mcpBefore -or (Get-TreeSnapshot $fixture.runtime) -cne $runtimeBefore -or (Get-TreeSnapshot $fixture.handoff) -cne $handoffBefore) { throw 'Preflight install modified a destination or protected state.' }
    if ((Get-StageSnapshot "$($fixture.game)/BepInEx") -cne $pluginStageBefore -or (Get-StageSnapshot ([IO.Path]::GetDirectoryName($fixture.mcp))) -cne $mcpStageBefore) { throw 'Preflight-only created staging state.' }
    $testCases++

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    $pluginBefore=Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright"
    $mcpBefore=Get-TreeSnapshot $fixture.mcp
    $runtimeBefore=Get-TreeSnapshot $fixture.runtime
    $handoffBefore=Get-TreeSnapshot $fixture.handoff
    $pluginStageBefore=Get-StageSnapshot "$($fixture.game)/BepInEx"
    $mcpStageBefore=Get-StageSnapshot ([IO.Path]::GetDirectoryName($fixture.mcp))
    $stageResult=& "$($fixture.package)/install.ps1" -DspDir $fixture.game -McpDestination $fixture.mcp -Force -StageOnly | ConvertFrom-Json
    if ($stageResult.installed -or -not $stageResult.staged -or -not $stageResult.integrityVerified -or -not $stageResult.exactFileSet -or $stageResult.transactionalUpgrade) { throw 'Invalid stage-only result.' }
    if ([IO.Path]::GetFullPath([string]$stageResult.pluginStagedTo).StartsWith(([IO.Path]::GetFullPath("$($fixture.game)/BepInEx/plugins").TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)) { throw 'Plugin staging entered the BepInEx/plugins scan range.' }
    Assert-ExactStagedSet $fixture $manifest $stageResult
    $pluginChanged = (Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright") -cne $pluginBefore
    $mcpChanged = (Get-TreeSnapshot $fixture.mcp) -cne $mcpBefore
    $runtimeChanged = (Get-TreeSnapshot $fixture.runtime) -cne $runtimeBefore
    $handoffChanged = (Get-TreeSnapshot $fixture.handoff) -cne $handoffBefore
    $pluginStageMissing = [string]::Equals((Get-StageSnapshot "$($fixture.game)/BepInEx"),$pluginStageBefore,[StringComparison]::Ordinal)
    $currentMcpStage = Get-StageSnapshot ([IO.Path]::GetDirectoryName($fixture.mcp))
    $mcpStageMissing = [string]::Equals($currentMcpStage,$mcpStageBefore,[StringComparison]::Ordinal)
    if ($pluginChanged -or $mcpChanged -or $runtimeChanged -or $handoffChanged -or $pluginStageMissing -or $mcpStageMissing) { throw "Stage-only invariant failed plugin=$pluginChanged mcp=$mcpChanged runtime=$runtimeChanged handoff=$handoffChanged pluginStageMissing=$pluginStageMissing mcpStageMissing=$mcpStageMissing" }
    $testCases++

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    Reject-Stage $fixture 'mutually exclusive' $fixture.mcp $fixture.game -AlsoPreflight

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    Reject-Stage $fixture 'Staging parent must already exist' (Join-Path $fixture.root 'missing-parent/mcp')

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    $stageMcp=Join-Path "$($fixture.game)/BepInEx/plugins" 'synthetic-mcp'
    [void][IO.Directory]::CreateDirectory($stageMcp)
    Reject-Stage $fixture 'Plugin staging must remain outside' $stageMcp

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    [void][IO.Directory]::CreateDirectory("$($fixture.game)/BepInEx/.spherewright-stage-residue-plugin")
    Reject-Stage $fixture 'prior Spherewright staging residue' $fixture.mcp

    foreach ($faultAfter in @(3, 5)) {
        # Fail inside Plugin staging, then separately on the first MCP copy
        # after all four Plugin assemblies have already staged successfully.
        $fixture=New-Fixture; $manifest=Write-Manifest $fixture
        $global:SpherewrightSyntheticCopyFaultCount=0
        $global:SpherewrightSyntheticCopyFaultAfter=$faultAfter
        $pluginBefore=Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright"
        $mcpBefore=Get-TreeSnapshot $fixture.mcp
        $runtimeBefore=Get-TreeSnapshot $fixture.runtime
        $handoffBefore=Get-TreeSnapshot $fixture.handoff
        $faulted=$false
        try { & "$($fixture.package)/install.ps1" -DspDir $fixture.game -McpDestination $fixture.mcp -Force -StageOnly | Out-Null } catch { if ($_.Exception.Message -notlike '*Synthetic staging copy failure*') { throw }; $faulted=$true }
        $global:SpherewrightSyntheticCopyFaultAfter=0
        $expectedMcpResidues=if ($faultAfter -eq 5) { 1 } else { 0 }
        if (-not $faulted -or (Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright") -cne $pluginBefore -or (Get-TreeSnapshot $fixture.mcp) -cne $mcpBefore -or (Get-TreeSnapshot $fixture.runtime) -cne $runtimeBefore -or (Get-TreeSnapshot $fixture.handoff) -cne $handoffBefore -or @(Get-ChildItem -LiteralPath "$($fixture.game)/BepInEx" -Force -Filter '.spherewright-stage-*').Count -ne 1 -or @(Get-ChildItem -LiteralPath $fixture.root -Force -Filter '.spherewright-stage-*-mcp').Count -ne $expectedMcpResidues) { throw 'Stage-copy failure did not preserve live/protected state and inspectable staging residues.' }
        $testCases++
    }

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    Reject-Stage $fixture 'must not overlap' $fixture.package

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    $sourceTarget=Join-Path $fixture.root 'stage-source-reparse-target'; [void][IO.Directory]::CreateDirectory($sourceTarget)
    New-TestJunction "$($fixture.package)/mcp/reparse" $sourceTarget
    Reject-Stage $fixture 'Reparse points are forbidden' $fixture.mcp

    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    $reparseTarget=Join-Path $fixture.root 'stage-destination-reparse-target'; [void][IO.Directory]::CreateDirectory($reparseTarget)
    $reparseParent=Join-Path $fixture.root 'stage-destination-reparse-parent'; New-TestJunction $reparseParent $reparseTarget
    Reject-Stage $fixture 'Reparse-point installation destinations are forbidden' (Join-Path $reparseParent 'mcp')

    foreach ($bad in @('missing-mcp','unlisted','duplicate','traversal','size','hash','source-extra-plugin','source-reparse')) {
        $fixture=New-Fixture; $manifest=Write-Manifest $fixture
        switch ($bad) {
            'missing-mcp' { Remove-Item -LiteralPath "$($fixture.package)/mcp/Spherewright.Mcp.exe"; $manifest=Write-Manifest $fixture; $expected='Required release file is missing' }
            'unlisted' { [IO.File]::WriteAllText("$($fixture.package)/mcp/extra.dll", 'unlisted'); $expected='Unlisted release file' }
            'duplicate' { $manifest.files += $manifest.files[0]; $expected='unsafe file path' }
            'traversal' { $manifest.files[0].path='../outside'; $expected='unsafe file path' }
            'size' { $manifest.files[0].size++; $expected='size verification failed' }
            'hash' { $manifest.files[0].sha256='00'; $expected='integrity verification failed' }
            'source-extra-plugin' { [IO.File]::WriteAllText("$($fixture.package)/BepInEx/plugins/Spherewright/extra.dll", 'listed'); $manifest=Write-Manifest $fixture; $expected='exactly four' }
            'source-reparse' { $target=Join-Path $fixture.root 'source-reparse-target'; [void][IO.Directory]::CreateDirectory($target); New-TestJunction "$($fixture.package)/mcp/reparse" $target; $expected='Reparse points are forbidden' }
        }
        Save-Manifest $fixture $manifest
        Reject-Install $fixture $expected
    }

    foreach ($bad in @('target-extra-plugin','target-extra-mcp','target-nested-test','target-child-reparse')) {
        $fixture=New-Fixture; $manifest=Write-Manifest $fixture
        switch ($bad) {
            'target-extra-plugin' { [IO.File]::WriteAllText("$($fixture.game)/BepInEx/plugins/Spherewright/old-plugin.dll", 'old-extra') }
            'target-extra-mcp' { [IO.File]::WriteAllText("$($fixture.mcp)/old-mcp.dll", 'old-extra') }
            'target-nested-test' { [void][IO.Directory]::CreateDirectory("$($fixture.mcp)/tests"); [IO.File]::WriteAllText("$($fixture.mcp)/tests/old-test.dll", 'old-test') }
            'target-child-reparse' { $target=Join-Path $fixture.root 'target-reparse-child'; [void][IO.Directory]::CreateDirectory($target); New-TestJunction "$($fixture.mcp)/old-link" $target }
        }
        $expected=if ($bad -eq 'target-child-reparse') { 'Reparse-point installation destinations are forbidden' } elseif ($bad -eq 'target-nested-test') { 'Unapproved installation directory' } else { 'Unapproved installation file' }
        Reject-Install $fixture $expected
    }

    $fixture=New-Fixture; $null=Write-Manifest $fixture
    Reject-Install $fixture 'must not overlap' $fixture.package
    $fixture=New-Fixture; $null=Write-Manifest $fixture
    Reject-Install $fixture 'must not overlap' "$($fixture.game)/BepInEx/plugins/Spherewright"
    $fixture=New-Fixture
    [void][IO.Directory]::CreateDirectory("$($fixture.package)/BepInEx/core")
    [IO.File]::WriteAllText("$($fixture.package)/BepInEx/core/BepInEx.dll", 'synthetic-package-core')
    $null=Write-Manifest $fixture
    Reject-Install $fixture 'must not overlap' $fixture.mcp $fixture.package
    $fixture=New-Fixture; $fixture=Set-PackageAtPluginPath $fixture ''
    $null=Write-Manifest $fixture
    Reject-Install $fixture 'must not overlap'
    $fixture=New-Fixture; $fixture=Set-PackageAtPluginPath $fixture 'payload'
    $null=Write-Manifest $fixture
    Reject-Install $fixture 'must not overlap'
    $fixture=New-Fixture; $null=Write-Manifest $fixture
    $reparseRoot=Join-Path $fixture.root 'destination-reparse-target'; [void][IO.Directory]::CreateDirectory($reparseRoot)
    $reparseParent=Join-Path $fixture.root 'destination-reparse-parent'; New-TestJunction $reparseParent $reparseRoot
    Reject-Install $fixture 'Reparse-point installation destinations are forbidden' (Join-Path $reparseParent 'mcp')

    $fixture=New-Fixture; $null=Write-Manifest $fixture
    $runtimeBefore=Get-TreeSnapshot $fixture.runtime; $handoffBefore=Get-TreeSnapshot $fixture.handoff
    $result=& "$($fixture.package)/install.ps1" -DspDir $fixture.game -McpDestination $fixture.mcp -Force | ConvertFrom-Json
    if (-not $result.integrityVerified -or -not $result.exactTargetFileSet -or [IO.File]::ReadAllText("$($fixture.mcp)/Spherewright.Mcp.exe") -cne 'synthetic-not-executable') { throw 'Synthetic install failed' }
    Assert-ExactInstalledSet $fixture $manifest
    if ((Get-TreeSnapshot $fixture.runtime) -cne $runtimeBefore -or (Get-TreeSnapshot $fixture.handoff) -cne $handoffBefore) { throw 'Install modified protected runtime or handoff state.' }
    $testCases++
    [pscustomobject]@{passed=$testCases;gameCalls=0;scope='synthetic preflight and copy only; not transactional/crash/real-package validation'} | ConvertTo-Json
} finally {
    $resolved=[IO.Path]::GetFullPath($testRoot)
    $allowed=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -notmatch '^spherewright-install-tests-[0-9a-f]{32}$') { throw 'Refusing unsafe test cleanup' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
