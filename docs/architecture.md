# Architecture

Spherewright has four boundaries:

1. `Spherewright.Contracts` contains stable wire DTOs and error codes.
2. `Spherewright.Bridge.Core` contains framing, authentication, bounded queues, plans, idempotency, state/resource fingerprints, action states, and quarantine logic without game assemblies.
3. `Spherewright.Plugin` is a thin BepInEx 5 adapter. It owns the secure Named Pipe and accesses DSP only from Unity's main thread.
4. `Spherewright.Mcp` exposes structured tools plus a small embedded Agent playbook resource over MCP stdio and never references game DLLs.

```text
External Agent
  -> inspect structured state
  -> select a goal and use game-independent deterministic planning helpers
  -> prepare one normal-game action
  -> commit with plan token and idempotency key
  -> poll action/read back state
  -> decide the next action
```

The Pipe worker parses bounded DTO envelopes and enqueues immutable commands. `SpherewrightBridgeHost.PumpMainThread` updates session identity and runs a bounded number of commands inside a frame budget. Results leave the main thread only as Spherewright-owned deep copies.

Movement planning remains outside the Plugin. `prepare_move` validates one surface target but does not inspect a future collision corridor. The Plugin's 180/600-tick watchdog terminates physical or route stalls, aborts only its exact owned order, and returns structured bounded-recovery fields. The MCP resource `spherewright://agent/playbooks/opening-movement-v1` turns the validated operating experience into host-readable guidance; all suggested escape candidates still go through normal fresh-read, prepare, commit, and terminal polling.

Overseer keeps separate bounded snapshot stores for production, cross-domain summaries, and the combined diagnostic bundle. A fresh bundle executes both captures synchronously inside one dispatched main-thread command, then the game-independent composer admits a planet only when both DTOs share the exact factory/planet identity, runtime flags, and game tick. Only this allowlisted joined DTO is retained for continuation pages; the bundle does not carry a game object or introduce another persistence source.

Foundry is part of the unreleased v0.4.0 scope alongside Overseer and Governor, preparing the departure star system for v0.5 Voyager. Its first implemented slice captures current unlocked recipes and building speed/power on the owned local planet, then passes only copied catalogs into the Core material compiler. The compiler reuses the dependency graph, aggregates shared input demand and returns a deterministic non-executable draft. It does not choose a player goal, create entities or retain write tokens. Site-bound immutable action graphs and protected restartable construction progress remain the next implementation boundary. External Agents choose the finite plan/site/goal; a bounded local executor may advance that approved work without repeated model decisions per belt, retaining per-object results rather than assuming atomic completion.

Optional Foundry site previews use a second bounded Core helper: at most 32 machine poses in the adapter-supplied spherical frame, shape-aware collider spheres and post-snap pairwise clearance. Plugin alone snaps to DSP's grid and shares ordinary build preparation's isolated native validator, using copied real inventory. Core separately checks the aggregate package budget and fingerprints the observation. No previews are committed, no write tokens or durable graphs are issued, and a clear machine layout does not prove logistics, power or production.

Governor's original v0.6 scope is now a v0.4 development target, not an autonomous controller. The initial read-only proposal adapter now joins explicit fresh owned selections with the existing same-tick Overseer diagnostic capture and Core Foundry compiler. Core compares native-upgrade, added-machine and module-copy costs/nameplate gains while keeping demand, actual production/consumption, local surplus and selected-buffer changes separate. At most eight source-bound observation series per session retain three independent 600-tick measurements; source/scale changes, restart or gaps cannot reuse a stale baseline. Planet counters require full direct-producer selection for line attribution. Proposals remain non-executable and never certify balance; complete extra-power/transport/replacement plans and live ten-minute expansion acceptance remain unfinished. The passive throughput observation mechanism is implemented separately below. Every proposal declares baseline, target, tolerance and game-time validation window; Agents preserve the pre-execution declaration and choose ordinary actions or bounded modules. Governor never starts another expansion. New factory planning, existing-line optimization and departure readiness are one release; actual interstellar route/warp control remains v0.5, later Dyson and release-candidate work v0.6/v0.7, and v1.0 promotion is unchanged.

Save privacy is based on creation provenance, explicit handoff, and exact object identity. The Plugin arms its own new-game transition and binds the resulting `GameData`; an unrelated loaded session otherwise receives restricted status without save/player/planet/factory reads. For a player-requested handoff, prepare captures only the exact current process/session/revision/`GameData` reference and returns a generic disclosure. The Agent must then ask in the conversation; only a subsequent explicit confirmation allows commit to generate a new internal name, normally save a copy, prove its exact header tick, and adopt it. The import API never accepts, returns, enumerates, loads, overwrites, renames, or deletes the original identity, and the attached-save journal begins at that boundary. Post-M0 flight recovery is limited to the separate checkpoint Spherewright itself creates immediately before launch: an internal name and lifecycle-bounded protected token bind its exact tick, primary owned identity, origin and destination, and no public request can supply a save name. The token is reloadable only for a failed/interrupted flight, expires after 24 hours, disappears on stable success, and retires after a covering primary save.

All gameplay mutations must use current-version normal business paths:

```text
walk on the current planet (no coordinate write/teleport)
harvest a reachable target (no inventory add)
queue replicator work (real ingredients and time)
create legal prebuilds from owned items
wait for construction drones/game ticks (no direct BuildFinally)
configure recipes/research through validated UI-equivalent paths
configure an idle empty sorter's item filter through the verified current-version UI assignment path
move owned fuel into the mecha fuel chamber through the native transfer path
save only the exact current process-owned world through DSP's normal save API
observe production, logistics, power, and technology
```

Post-M0 same-star flight remains an ordinary game-tick action. Its commit first proves a separate internal checkpoint, then uses native `Fly`/`Sail` transitions and paid sail-energy adjustments; it never assigns player position or performs a planet transfer. Exact checkpoint reload is a separate two-stage mutation that may replace only the matching owned game and remains repeatable until that flight succeeds.

The external Agent composes these primitives to build production lines and advance research. Spherewright does not contain an autonomous goal planner or a one-click completion method; the historical first-red-matrix run is an earlier example of this boundary.

The older sandbox basic-line coordinator is a historical experiment. Its source is retained for API provenance, but the Plugin project explicitly removes it from compilation. Its item grants, direct `BuildFinally`, and storage insertion are absent from the current binary and public MCP surface; Foundry must not revive that implementation.

## Finite module data and upgrade execution

The 0.4 authorization includes bounded native module construction, not an internal expansion planner. Read-only blueprint data/site tools remain non-executable. The separate finite executor reuses NormalGameActionCoordinator, session/revision/GameData checks, plan/idempotency stores and quarantine. Core BlueprintBuildState holds immutable geometry/connections and per-object receipt state, without selecting goals or invoking game APIs. Plugin translates via native blueprint geometry and submits one dependency-ready object through existing native Click/Path/Inserter tools per tick, retaining normal costs/drones. Protected BlueprintBuildStore atomically flushes write-ahead and result evidence per exact owned identity. Restart restores no token: readback of known entities, material receipts and reciprocal/free-end topology precedes a fresh prepare. Cancel retains submitted work; unknown outcomes cannot replay. Source/offline capability and live acceptance remain distinct.

The first upgrade slice reuses `NormalGameActionCoordinator`, `PreparedPlanStore`, session/revision checks, idempotency and outcome-unknown quarantine. Core checks supported families and readback conservation; Plugin alone calls native `DoUpgradeObject` and reads live configuration/cargo/reciprocal links. It never directly assigns prototype IDs or assumes a successful whole-factory expansion. Governor must compare measured demand/supply deficits and upgrade/new/copy alternatives before an external Agent chooses an action.

Foundry power assessment receives only deep-copied bounded native node/network/consumer evidence. Core groups proposed network joins and full base-load budgets; Plugin never builds a node or assigns a consumer while reading. Its current-capture hash is separate from the unchanged native construction assessment, and pre-existing unfinished prebuilds make it unavailable rather than silently omitting future loads. Governor similarly reuses same-tick Overseer evidence and one static source binding; its at-most8 predeclared passive throughput observers are non-durable session state, not owned construction records or executable plans. Target-path and unattributed global findings remain separate; all gameplay execution stays in existing finite prepare/commit paths.

## Validation deployment

Foundry's explicit-blueprint composition reuses the same finite native site and `BlueprintBuildState`, not another planning/execution loop. Core binds runtime material intent to exact stage objects, directed item-compatible flow allocations, free external boundary conditions and ALL object costs/dependencies. Plugin fresh-prepare/commit rechecks the graph and power; protected progress retains the original intent/hash without restoring any token. Resume budgets only remaining objects against current native power. This does not turn the machine grid into arbitrary routing or prove runtime transport rates.

Current development and structured live verification use the local Windows computer. A separate same-LAN game-validation computer is a deferred deployment architecture, not an implemented v0.4 feature. Its Named Pipe would remain local to the game computer while deployment, remote MCP stdio, and sanitized evidence collection use an authenticated remote command channel.

See [remote-validation.md](./remote-validation.md) for the historical proposal. Implementing that deployment architecture requires the user to explicitly resume it; completing M0 did not implicitly authorize it.
