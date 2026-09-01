[CmdletBinding()]
param(
    [ValidateRange(1, 72)][int]$LifetimeHours = 24
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SpherewrightBridgeClient.ps1')

function Get-RuleSid([System.Security.AccessControl.FileSystemAccessRule]$Rule) {
    return $Rule.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value
}

function Assert-CurrentUserOnlyAcl([string]$Path) {
    $currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    if ($null -eq $currentSid) {
        throw 'The current Windows user SID is unavailable.'
    }

    $acl = Get-Acl -LiteralPath $Path
    if (-not $acl.AreAccessRulesProtected) {
        throw 'The planned-restart handoff path still inherits access rules.'
    }

    $foreignAllows = @($acl.Access | Where-Object {
        $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow `
            -and (Get-RuleSid $_) -ne $currentSid.Value
    })
    if ($foreignAllows.Count -ne 0) {
        throw 'The planned-restart handoff path grants access to another SID.'
    }
}

function Protect-NewCurrentUserFile([string]$Path) {
    $currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    if ($null -eq $currentSid) {
        throw 'The current Windows user SID is unavailable.'
    }

    $acl = Get-Acl -LiteralPath $Path
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($rule in @($acl.Access)) {
        $acl.RemoveAccessRuleSpecific($rule)
    }

    $acl.SetOwner($currentSid)
    $acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new(
        $currentSid,
        [System.Security.AccessControl.FileSystemRights]::FullControl,
        [System.Security.AccessControl.AccessControlType]::Allow))
    Set-Acl -LiteralPath $Path -AclObject $acl
    Assert-CurrentUserOnlyAcl -Path $Path
}

$descriptor = Get-LiveSpherewrightDescriptor
$sessionResponse = Invoke-SpherewrightBridgeRequest -Method 'get_session_state' -Payload @{} -Descriptor $descriptor
$session = Get-SpherewrightBridgeResult -Response $sessionResponse -Operation 'get_session_state'
if (-not $session.gameLoaded `
    -or -not $session.ownedBySpherewright `
    -or $session.writeHealth -ne 'healthy' `
    -or $session.ownedSaveState -ne 'saved' `
    -or [string]::IsNullOrWhiteSpace([string]$session.saveName) `
    -or [string]::IsNullOrWhiteSpace([string]$session.sessionId) `
    -or $null -eq $session.localPlanetId `
    -or [int]$session.localPlanetId -le 0 `
    -or $null -eq $session.lastOwnedSaveGameTick `
    -or [long]$session.lastOwnedSaveGameTick -lt 0 `
    -or $session.peacefulMode -ne 'confirmed_peaceful' `
    -or $session.sandboxMode -ne 'confirmed_disabled' `
    -or [Math]::Abs([double]$session.resourceMultiplier - 1.0) -gt 0.0001) {
    throw 'A healthy, normally saved, owned peaceful 1x session is required before arming planned restart.'
}

$gameProcess = Get-Process -Id ([int]$descriptor.processId) -ErrorAction Stop
if ($gameProcess.ProcessName -ne 'DSPGAME' -or $gameProcess.HasExited) {
    throw 'The live Bridge descriptor does not belong to the running DSP process.'
}

$gameRoot = [IO.Path]::GetFullPath((Split-Path -Parent $gameProcess.Path))
$pluginDirectory = [IO.Path]::GetFullPath((Join-Path $gameRoot 'BepInEx\plugins\Spherewright'))
$handoffDirectory = [IO.Path]::GetFullPath((Join-Path $pluginDirectory 'runtime-handoff'))
if (-not $handoffDirectory.StartsWith($pluginDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) `
    -or -not (Test-Path -LiteralPath $handoffDirectory -PathType Container)) {
    throw 'The fixed Spherewright runtime-handoff directory is unavailable.'
}

Assert-CurrentUserOnlyAcl -Path $handoffDirectory
$ticketPath = Join-Path $handoffDirectory 'owned-world-resume.json'
if (Test-Path -LiteralPath $ticketPath) {
    throw 'A planned-restart handoff ticket already exists; it must be consumed or inspected before another is armed.'
}

$tokenBytes = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($tokenBytes)
$resumeToken = [Convert]::ToBase64String($tokenBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$issuedAt = [DateTimeOffset]::UtcNow
$ticket = [ordered]@{
    version = 1
    resumeToken = $resumeToken
    ownedSaveName = [string]$session.saveName
    sourceSessionId = [string]$session.sessionId
    sourceProcessId = [int]$descriptor.processId
    sourceBridgeInstanceId = [string]$descriptor.bridgeInstanceId
    gameVersion = [string]$session.gameVersion
    expectedPlanetId = [int]$session.localPlanetId
    minimumGameTick = [long]$session.lastOwnedSaveGameTick
    quarantineActionId = ''
    issuedAtUtc = $issuedAt
    expiresAtUtc = $issuedAt.AddHours($LifetimeHours)
}

$ticketBytes = [Text.UTF8Encoding]::new($false).GetBytes(($ticket | ConvertTo-Json -Depth 4 -Compress))
$temporaryPath = Join-Path $handoffDirectory ('.owned-world-resume-' + [guid]::NewGuid().ToString('N') + '.tmp')
try {
    $stream = [IO.FileStream]::new(
        $temporaryPath,
        [IO.FileMode]::CreateNew,
        [IO.FileAccess]::Write,
        [IO.FileShare]::None,
        4096,
        [IO.FileOptions]::WriteThrough)
    try {
        $stream.Write($ticketBytes, 0, $ticketBytes.Length)
        $stream.Flush($true)
    } finally {
        $stream.Dispose()
    }

    Protect-NewCurrentUserFile -Path $temporaryPath
    Move-Item -LiteralPath $temporaryPath -Destination $ticketPath
    Assert-CurrentUserOnlyAcl -Path $ticketPath
} finally {
    if (Test-Path -LiteralPath $temporaryPath) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
}

[pscustomobject]@{
    armed = $true
    expectedPlanetId = [int]$session.localPlanetId
    minimumGameTick = [long]$session.lastOwnedSaveGameTick
    expiresAtUtc = $issuedAt.AddHours($LifetimeHours)
    currentUserOnlyAcl = $true
}
