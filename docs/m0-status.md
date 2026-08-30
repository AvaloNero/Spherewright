# M0 status — First Red Matrix

Scope revised: 2026-08-30 (Asia/Shanghai).

Execution decision: continue M0 development and DSP verification on the local computer. The LAN game-host topology is documented in `docs/remote-validation.md` and explicitly deferred until the local first-red-matrix run is repeatable.

| Gate | Status | Evidence / earliest gap |
|---|---|---|
| Gate A — environment, load, secure status | complete | DSP `0.10.34.28529`; BepInEx loaded once per process; protected current-user ACL and token rejection verified; MCP Inspector called the stdio server; normal exit removed the descriptor. Revalidate after protocol surface changes, but the foundation is complete. |
| Gate B — ordinary owned world and structured observation | complete | Fresh action `4cc929fb-c5c0-4e59-96e6-e9cb3c5940a8` created an owned peaceful 1x non-sandbox world. Live session/player/progression/catalog/resource/factory/power reads, resource live-inspect, cursor binding, action-result replay, and new-game idempotency all passed against the installed DLL. |
| Gate C — normal-play action primitives | in-progress | One process-owned ordinary test world verified structured research progression and legal drone-built power/mining/logistics/smelting, including a real miner-to-belt-to-sorter-to-smelter flow. Native refuel, explicit owned-save, research-result acknowledgement, and idle sorter-filter configuration are implemented and compile-tested but are not installed/runtime-verified yet. |
| Gate D — first red matrix | not-started | Requires the remaining Gate C runtime checks, then one uninterrupted fresh-save run with structured provenance, explicit save confirmation, and red-matrix readback. Per user instruction, execution is paused before installing/restarting/creating that next validation save and requires explicit confirmation. |

The earliest incomplete gate is **Gate C**. The next runtime boundary is deliberately paused until the user confirms a new-save validation run.

## Historical evidence excluded from revised M0

The earlier sandbox basic-line actions `9170d11c-e458-4d11-b1d1-a406b818d890` and `e77fd86a-2e5f-474b-a1a4-f845252be221` proved several current-version build, recipe, connection, power, idempotency, and readback call paths. They also granted building items, called `BuildFinally`, and inserted ore directly. They are research evidence only and do not satisfy ordinary-gameplay Gates B, C, or D. `Spherewright.Plugin.csproj` explicitly excludes their coordinator from the current Plugin binary.

No pre-existing save has been enumerated, loaded, or read.

## Current ordinary-play evidence

- Non-final primitive session `22962c57-398b-4f80-b4e5-23eef9ece284` on planet `103` was created and owned by the current Spherewright process; it is development evidence, not the future Gate D run.
- Early technologies `1001`, `1002`, `1201`, `1401`, and `1601` completed through normal research progression.
- Normal owned-item construction produced a wind turbine, matrix lab, mining machine, power pole, a six-segment iron belt (action `e21ed435-73f8-4314-8fc4-828402451fc2`), a smelter (entity `11`), and a sorter (entity `12`).
- Structured readback observed the real miner-to-belt-to-sorter-to-smelter chain consuming ore and increasing iron ingots under its measured power ratio; no miner output or production buffer was injected.
- This world is intentionally not being extended into acceptance evidence: the new Plugin build must be installed in a later process, and the user requires confirmation before the next fresh save is created.

## Current offline verification

- `dotnet build Spherewright.sln --no-restore`: succeeded, 0 warnings, 0 errors.
- `dotnet test Spherewright.Core.slnf --no-build`: 38 passed (Contracts 3, Bridge.Core 26, MCP 9).
- MCP assembly-registration test expects exactly 34 public tools and explicitly rejects any `basic_production_line` registration.
- ILSpy class listing of the rebuilt Plugin contains `NormalGameActionCoordinator` and the ordinary `TestWorldCoordinator`, but no `BasicProductionLineCoordinator`.
- Fresh ordinary-world verification used process `36496`, session `3d84bb3d-1e15-497f-901a-3bf8375490f1`, and local planet `103`. Readback confirmed `confirmed_peaceful`, `confirmed_disabled`, resource multiplier `1.0`, empty initial inventory, empty handcraft queue, 314 technology states, 161 recipes, 174 items, and runtime red-matrix item `6002` with five dependency recipes.
- The same-key new-game commit replay returned the original action with `idempotentReplay=true`; a resource cursor reused under a different filter returned `STALE_CURSOR`.
- BepInEx and DSP `Player.log` contained no error or exception after the successful run.
