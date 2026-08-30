[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command npx -ErrorAction SilentlyContinue)) {
    throw 'npx is required to run the MCP Inspector.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
npx -y '@modelcontextprotocol/inspector' dotnet run --project (Join-Path $repoRoot 'src\Spherewright.Mcp\Spherewright.Mcp.csproj') --no-build

