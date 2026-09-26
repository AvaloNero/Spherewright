[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$PluginDestination,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$McpDestination,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$OperationId,
    [switch]$RestoreOriginal,
    [string]$ExpectedEvidenceHash
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$hasExpectedEvidenceHash = $PSBoundParameters.ContainsKey('ExpectedEvidenceHash')
if ($RestoreOriginal) {
    if (-not $hasExpectedEvidenceHash -or [string]::IsNullOrWhiteSpace($ExpectedEvidenceHash)) {
        throw '-RestoreOriginal requires -ExpectedEvidenceHash from a matching recovery preview.'
    }
} elseif ($hasExpectedEvidenceHash) {
    throw '-ExpectedEvidenceHash is only valid together with -RestoreOriginal.'
}

. (Join-Path $PSScriptRoot 'SpherewrightInstallRecovery.ps1')

$result = if ($RestoreOriginal) {
    Invoke-SpherewrightInstallRecovery `
        -PluginDestination $PluginDestination `
        -McpDestination $McpDestination `
        -OperationId $OperationId `
        -ExpectedEvidenceHash $ExpectedEvidenceHash
} else {
    Get-SpherewrightInstallRecoveryPreview `
        -PluginDestination $PluginDestination `
        -McpDestination $McpDestination `
        -OperationId $OperationId
}

$result | ConvertTo-Json -Depth 16 -Compress
