# Staged MCP metadata validation

## Implemented boundary

The manual package now includes `Test-SpherewrightStagedMcp.ps1`. After both payloads have staged, `install-release.ps1 -StageOnly` launches only the staged MCP executable and verifies initialization/version, tool discovery, resource discovery and the exact packaged opening playbook. It then rechecks both staged payloads against the manifest. A failed handshake or changed payload cannot report successful staging.

The child receives a private, nonexistent descriptor path with no fallback to the real Bridge, and an isolated child-only LOCALAPPDATA. Requests are a closed metadata sequence; no `tools/call` or gameplay request is sent. Output is bounded to 524288 stdout characters and 131072 stderr characters, with five seconds per request and a thirty-second protocol deadline. Cleanup targets only the child process started by this probe. Parent environment settings are unchanged.

## Offline evidence

- `scripts/test-install-release-preflight.ps1`: **34 synthetic cases passed**, `gameCalls=0`. These installation-control-flow fixtures use a stub probe. Missing helper/playbook, handshake rejection and post-probe payload tampering are rejected while live payload and protected runtime/handoff trees remain unchanged.
- `scripts/test-staged-mcp.ps1`, run with Windows PowerShell: **13 checks passed**. These include actual synthetic subprocesses for malformed protocol, timeout, nonzero exit, version/discovery/playbook mismatches and output limits, plus existing-isolation rejection and unchanged parent environment.
- The latter suite also ran the current source-built Release MCP executable: **64 tools, 1 resource, 89544 playbook characters**, exact playbook match and normal exit. This is source-built metadata evidence, not a packaged ZIP, installed Plugin, cold deployment, cross-computer or live-game result.
- Independent Sol review found no blocking issue; temporary fixture cleanup requires both the resolved temporary-parent prefix and the exact task-specific directory-name pattern.

## Follow-up: cross-shell value-shape regression

After the initial Windows PowerShell checks, PowerShell 7.6.5 rejected the valid initialize reply with `initialize serverInfo is missing 'version'`. A minimal reproduction showed `Write-Output -NoEnumerate` wrapping a scalar PSCustomObject in `List<object>`. The getter now returns a unary-comma value, preserving object identity as well as scalar, null, empty-array, singleton-array and multi-item-array shape. Six direct regressions were added. The complete probe suite now passes **19 checks independently under Windows PowerShell and PowerShell 7.6.5**, including the source-built MCP metadata checks above. This shell compatibility fix does not change MCP requests or installation authority.

## Remaining work (unchanged)

Subsequent follow-through: [caught-failure rollback](install-caught-failure-rollback.md) replaces the default direct-copy path. The paragraph below preserves the boundary at this metadata slice; process interruption recovery and final-package validation remain open after that follow-through as well.

This extends the [stage-only slice](../2026-09-26/install-release-stage-only.md), not the live installer transaction. Default installation still uses direct replacement and reports no transactional guarantee. Original-payload backup, coordinated promotion, verified rollback, interrupted-operation recovery and final same-commit package validation remain open. No game load/save/write, real-target installation, tag or release occurred in this slice.
