# Offline launch regression only. No Bridge, descriptor or game access.
param([switch]$Probe, [switch]$ProbeFailure)
$ErrorActionPreference = 'Stop'
$marker = 'spherewright_offline_entry_body'
if ($Probe) {
    [Console]::WriteLine($marker)
    if ($ProbeFailure) { exit 17 }
    exit 0
}

$pwsh = (Get-Command pwsh -ErrorAction Stop).Source
$fixture = @'
try {
    [Console]::WriteLine('spherewright_offline_entry_body')
} catch {
    exit 17
}
'@
# stdin is an observation, not a requirement that future PowerShell stay broken.
# A zero process exit without the marker must never prove an action ran/did not run.
$stdinOutput = @($fixture | & $pwsh -NoProfile -Command -)
$stdinExit = $LASTEXITCODE
$fileOutput = @(& $pwsh -NoProfile -File $PSCommandPath -Probe)
if ($LASTEXITCODE -ne 0 -or $fileOutput.Count -ne 1 -or $fileOutput[0] -cne $marker) {
    throw 'Physical pwsh -File entry did not reach the offline body.'
}
$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($fixture))
$encodedOutput = @(& $pwsh -NoProfile -EncodedCommand $encoded)
if ($LASTEXITCODE -ne 0 -or $encodedOutput.Count -ne 1 -or $encodedOutput[0] -cne $marker) {
    throw 'Explicit encoded entry did not reach the offline body.'
}
$failureOutput = @(& $pwsh -NoProfile -File $PSCommandPath -Probe -ProbeFailure)
if ($LASTEXITCODE -ne 17 -or $failureOutput.Count -ne 1 -or $failureOutput[0] -cne $marker) {
    throw 'Physical script entry did not propagate the known offline failure.'
}
[pscustomobject]@{
    passed = 3
    gameCalls = 0
    stdinExit = $stdinExit
    stdinBodyObserved = (@($stdinOutput | Where-Object { $_ -ceq $marker }).Count -eq 1)
    zeroExitProvesGameOutcome = $false
} | ConvertTo-Json -Compress
