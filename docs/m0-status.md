# M0 status — First Red Matrix

Scope revised: 2026-08-31 (Asia/Singapore).

Execution decision: continue M0 development and DSP verification on the local computer. The LAN game-host topology is documented in `docs/remote-validation.md` and explicitly deferred until the local first-red-matrix run is repeatable.

| Gate | Status | Evidence / earliest gap |
|---|---|---|
| Gate A — environment, load, secure status | complete | DSP `0.10.34.28529`; BepInEx loaded once per process; protected current-user ACL and token rejection verified; MCP Inspector called the stdio server; normal exit removed the descriptor. Revalidate after protocol surface changes, but the foundation is complete. |
| Gate B — ordinary owned world and structured observation | complete | Current exact owned session `c51a4fd0-5a50-4fb2-8520-f7acf216334d`, planet `104`, is confirmed peaceful, 1x, non-sandbox, write-healthy. It is the same Spherewright-owned world recovered through the strict one-time LastExit provenance path; no unrelated save was enumerated or loaded. |
| Gate C — normal-play action primitives | complete | Movement, harvesting, handcrafting, research, drone construction, belts, sorters, production configuration, transfer, refuel and explicit save all have live evidence. The co-located-sorter attribution fix is live-validated by distinct refinery outputs `164 -> 163` and `181 -> 170`, with action ownership resolving only the new entity and session health remaining `healthy`. |
| Gate D — first red matrix | complete | Lab `256` was configured to runtime recipe `18` with output item `6002` at 0, then normally fed by graphite sorter `257` and hydrogen sorter `258`; output grew `0 -> 3 -> 6`. Save action `b399facb-48cd-4838-b7ab-9c9762b6def7` confirmed the exact owned save at tick `2499658`, revision `150 -> 151`, with `ownedSaveState=saved` and `writeHealth=healthy`. |

The M0 milestone is **complete**. The first post-M0 production improvement is network 2 capacity: the running red-matrix station raises demand to roughly `16.4k–16.7k` against `15k` capacity, so it operates at about 90% speed. Yellow-matrix planning remains downstream work and must not retroactively weaken the completed M0 evidence.

## Post-M0 continuation state and evidence boundary

- The live process still owns the same session and planet. After later normal transfers, refuels, moves and a second explicit save, session revision is `191`, `writeHealth=healthy`, `ownedSaveState=saved`, and `lastOwnedSaveGameTick=2710106`. `restartResumeAvailable=false`, so the running Plugin must not be replaced or the game closed on the assumption that the tool can already resume it.
- Lab `256` remains recipe `18`; its energy-matrix output continued from the accepted `0 -> 3 -> 6` proof to `10`, with graphite and hydrogen inputs both at 6. It is currently idle because its own output buffer is full, not because the upstream line or power failed. Normal save action `02f50a58-276c-4b90-be62-bb9645920abf` preserved this continuation state at tick `2710106`.
- A later movement collision occurred near the dense network-1 factory. The old deployed Plugin waited for its global movement timeout even after repeated reads proved no displacement. One post-M0 Computer Use jump was then used to recover the already completed/saved world and distinguish collision from target or energy failure. This input is explicitly outside the M0 acceptance boundary and is not evidence for a structured movement capability.
- Source now contains a 180-tick physical-stall watchdog, a 600-tick best-target-progress watchdog, power-starvation classification isolation, and move/harvest single-flight. These changes build and test offline but are not live in the current process; deploy only on a later restart whose exact same-save recovery can first be proven.
- Per user workflow, continue this same save while it remains healthy. Every newly completed product line must be verified in its producer, saved normally, then paired with one Git commit and push containing its implementation and updated experience evidence.

## Historical evidence excluded from revised M0

The earlier sandbox basic-line actions `9170d11c-e458-4d11-b1d1-a406b818d890` and `e77fd86a-2e5f-474b-a1a4-f845252be221` proved several current-version build, recipe, connection, power, idempotency, and readback call paths. They also granted building items, called `BuildFinally`, and inserted ore directly. They are research evidence only and do not satisfy ordinary-gameplay Gates B, C, or D. `Spherewright.Plugin.csproj` explicitly excludes their coordinator from the current Plugin binary.

No pre-existing save has been enumerated, loaded, or read.

## Current ordinary-play evidence

- The M0 acceptance snapshot is the exact owned session `c51a4fd0-5a50-4fb2-8520-f7acf216334d` on planet `104` at revision `151` after the structured Gate-D save. Peaceful mode, non-sandbox mode, 1x resources, ownership and healthy writes are all structured readbacks. The same live session has since advanced to revision `191`; the post-M0 boundary above prevents later recovery inputs from being retroactively included in Gate D.
- The existing normal factory supplies electromagnetic matrices, graphite and crude oil. Oil extractor `129` feeds belts `151…161`, sorter `162`, refinery `141` recipe `16`, refined-oil sorter `164` and hydrogen sorter `181`.
- Coverage pole `182` moved crude-input sorter `162` from `network=0/Picking` to network 3 with actual crude cargo. Refined oil then grew in storage `163`; it also normally fuels thermal generator `183` through sorter `184`, raising network 3 capacity from `15000` to `51000` and restoring service ratio 1.0.
- Hydrogen leaves tank `165` through three ordinary belt-build actions: `49013865-bb55-4aca-899a-699e8d3744e7`, `3870525c-14ce-4e41-936c-36984d560858`, and `8f733ab2-a4da-4616-ae03-ee5778299ba3`, ending at belt `255` beside the red lab. Two one-item hydrogen returns caused by live-belt extension were separately recorded in player inventory and explicitly excluded from production evidence.
- Lab `256` began with recipe 18 buffers `graphite=0`, `hydrogen=0`, `6002=0`. Sorters `257` and `258` connected the 3000-count normal graphite storage `114` and hydrogen belt `255`; the same lab then showed inputs growing and energy matrices `0 -> 3 -> 6` while production progress advanced.
- Final save action `b399facb-48cd-4838-b7ab-9c9762b6def7` confirmed DSP's normal save API at tick `2499658`; it did not accept a client-provided save name and did not enumerate another save.
- The earlier session `73b4019b-c5cc-4f90-b1f4-bc4abc6d49c6` and its quarantine remain historical regression evidence only. Its failed attribution was repaired, deployed, and independently live-validated before the current completion evidence was accepted.

## Current offline verification

- Current takeover machine: portable SDK 8.0.424 builds the full `Spherewright.sln` with 0 warnings and 0 errors; 55 tests pass (Contracts 3, Bridge.Core 42, MCP 10).
- Current takeover machine: `dotnet restore Spherewright.sln --locked-mode --source https://api.nuget.org/v3/index.json` now succeeds after removing the SDK-dependent implicit .NET Framework umbrella package; the committed lock file remains unchanged. The explicit source is required because this user's global NuGet configuration is currently empty.
- With explicit user approval, official `xiaoye97-BepInEx-5.4.17` and the development Plugin were installed. DSP build ID `23109513` and `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85` match the verified baseline.
- Steam discovery now de-duplicates case-only variants of the same validated DSP root instead of reporting a false multiple-installation error.
- Idempotency entries now honor `IdempotencyRetentionMinutes`, reclaim expired capacity, and isolate identical UUIDs between sessions; a concurrent reservation test proves only one insertion wins within a session.
- Build completion snapshots pre-existing co-located sorter IDs before `CreatePrebuilds` and excludes them when attributing the constructed entity. The regression test reproduces old entity `211` and new entity `213`; the deployed fix was additionally live-validated by current entities `164/181` at one source pose with distinct destinations and correct new-action ownership.
- MCP assembly-registration test expects exactly 38 public tools, including the exact-proof quarantine-reconciliation and one-time owned-world-resume pairs, and explicitly rejects any `basic_production_line` registration.
- ILSpy class listing of the rebuilt Plugin contains `NormalGameActionCoordinator` and the ordinary `TestWorldCoordinator`, but no `BasicProductionLineCoordinator`.
- Current-DLL inspection proves research-result `FadeOut()` immediately makes `ready` false (`alpha 1.0 -> 0.999`) before the native close animation, preventing repeated acknowledgement. Factory DTO and canonical hash coverage bound the live lab `256` before and after input, and runtime evidence now proves item `6002` changed from 0 to 6.
- Source now captures `TankComponent.fluidId/fluidCount/fluidInc` as `tank-fluid`; this read-only enhancement builds and tests cleanly but is intentionally not hot-deployed into the healthy completed session. Current M0 hydrogen proof instead uses the refinery output sorter, complete belt topology, lab input and output deltas.
- Source now detects powered movement stalls through independent physical-displacement and best-target-progress windows, preserves the dedicated power-starvation reason, and refuses a second simultaneous move/harvest player order. Five pure Core tests cover the watchdog boundaries; the full Plugin compile covers the integration, while live validation waits for a safe same-save restart.
- Fresh ordinary-world verification also confirmed 314 technology states, 161 recipes, 174 items, runtime red-matrix item `6002`, recipe `18`, and the runtime dependency recipe set `16, 17, 18, 32, 58, 74, 121`.
- The same-key new-game commit replay returned the original action with `idempotentReplay=true`; a resource cursor reused under a different filter returned `STALE_CURSOR`.
- The historical BepInEx log retains the expected earlier quarantine event for regression evidence. It is not confused with the current healthy session or used to weaken the live completion and final-save readbacks above.
