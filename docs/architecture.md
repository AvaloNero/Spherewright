# Architecture

Spherewright has four boundaries:

1. `Spherewright.Contracts` contains stable wire DTOs and error codes.
2. `Spherewright.Bridge.Core` contains framing, authentication, bounded queues, plans, idempotency, state/resource fingerprints, action states, and quarantine logic without game assemblies.
3. `Spherewright.Plugin` is a thin BepInEx 5 adapter. It owns the secure Named Pipe and accesses DSP only from Unity's main thread.
4. `Spherewright.Mcp` exposes structured tools over MCP stdio and never references game DLLs.

```text
External Agent
  -> inspect structured state
  -> deterministic planning outside the Plugin
  -> prepare one normal-game action
  -> commit with plan token and idempotency key
  -> poll action/read back state
  -> decide the next action
```

The Pipe worker parses bounded DTO envelopes and enqueues immutable commands. `SpherewrightBridgeHost.PumpMainThread` updates session identity and runs a bounded number of commands inside a frame budget. Results leave the main thread only as Spherewright-owned deep copies.

Save privacy is based on creation provenance and exact object identity. The Plugin arms only its own new-game transition and binds the resulting `GameData`; an unrelated loaded session receives restricted status without save/player/factory reads. M0 does not enumerate or load existing saves.

For M0, all gameplay mutations must use current-version normal business paths:

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

The external Agent composes these primitives to reach the first red matrix. Spherewright does not contain an autonomous red-matrix planner or a one-click completion method.

The older sandbox basic-line coordinator is a historical experiment. Its source is retained for API provenance, but the Plugin project explicitly removes it from compilation. Its item grants, direct `BuildFinally`, and storage insertion are absent from the M0 binary and public MCP surface.

## Validation deployment

M0 is currently developed and verified on the same local Windows computer. A separate same-LAN game-validation computer is a deferred deployment architecture, not current M0 scope. Its Named Pipe would remain local to the game computer while deployment, remote MCP stdio, and sanitized evidence collection use an authenticated remote command channel.

See [remote-validation.md](./remote-validation.md). Do not implement that architecture until the local first-red-matrix run is complete and the user explicitly resumes it.
