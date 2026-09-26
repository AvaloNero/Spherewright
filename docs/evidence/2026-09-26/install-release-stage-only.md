# Release installer stage-only slice

## Scope

`scripts/install-release.ps1 -StageOnly` completes the existing manifest, source, target-tree and reparse-point preflight, then copies the verified Plugin and MCP payloads only to separate, operation-ID-named staging roots. Plugin staging is derived under `BepInEx` but outside the recursive `BepInEx/plugins` scan range; MCP staging is separately derived beside its own live target. Each staged payload is verified against the exact declared file set and SHA-256 values before a result reports `installed=false`, `staged=true`, and `transactionalUpgrade=false`.

The slice does not replace a live Plugin or MCP target, copy, delete, move, or rename `runtime-handoff`, clean a prior staging directory, implement a write-ahead log, or claim atomic upgrade or crash recovery. A partial copy deliberately leaves its uniquely named staging residue available for inspection; a later attempt rejects that residue rather than overwriting it.

## Synthetic coverage

`scripts/test-install-release-preflight.ps1` passed 30 synthetic cases with `gameCalls=0`. They cover ordinary preflight with no staging, flag mutual exclusion, missing staging-parent rejection without creating staging state, successful separate Plugin/MCP staging with exact payload hashes, source and destination reparse rejection, staging outside the Plugin scan scope, overlap rejection, and prior staging-residue rejection. Copy failures are injected separately at the third Plugin copy and the first MCP copy after all four Plugin assemblies have staged; both preserve byte-identical live Plugin/MCP/runtime/handoff trees and leave inspectable staging residues. All fixtures use temporary synthetic directories; this is not a real installation or game validation.

## Boundary

The staged roots are only inspectable payload copies. Promotion, replacement ordering, rollback, and interruption recovery remain separate work and must not be inferred from this slice.

Both staging parents must already exist. In particular, a missing MCP destination parent is rejected before either payload is staged; this slice does not automatically create a first-install parent hierarchy.
