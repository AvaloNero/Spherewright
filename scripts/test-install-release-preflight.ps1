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
try {
    $fixture=New-Fixture; $manifest=Write-Manifest $fixture
    $pluginBefore=Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright"
    $mcpBefore=Get-TreeSnapshot $fixture.mcp
    $runtimeBefore=Get-TreeSnapshot $fixture.runtime
    $handoffBefore=Get-TreeSnapshot $fixture.handoff
    $result=& "$($fixture.package)/install.ps1" -DspDir $fixture.game -McpDestination $fixture.mcp -Force -PreflightOnly | ConvertFrom-Json
    if (-not $result.preflightOnly -or $result.installed -or -not $result.exactFileSet) { throw 'Invalid preview result' }
    if ((Get-TreeSnapshot "$($fixture.game)/BepInEx/plugins/Spherewright") -cne $pluginBefore -or (Get-TreeSnapshot $fixture.mcp) -cne $mcpBefore -or (Get-TreeSnapshot $fixture.runtime) -cne $runtimeBefore -or (Get-TreeSnapshot $fixture.handoff) -cne $handoffBefore) { throw 'Preflight install modified a destination or protected state.' }
    $testCases++

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
