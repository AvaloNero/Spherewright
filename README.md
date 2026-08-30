# Spherewright

Spherewright is a Windows-first, tool-first control bridge for Dyson Sphere Program. An external Agent talks to a local MCP server; the MCP server talks to a BepInEx 5 Plugin over an authenticated current-user Named Pipe. Spherewright does not embed an LLM.

The current milestone is **M0 — First Red Matrix**: after DSP reaches the main menu, an external Agent must use only structured Spherewright tools to create a fresh peaceful 1x save and produce at least one red matrix under normal game rules.

M0 explicitly forbids Computer Use, visual recognition, keyboard/mouse macros, sandbox mode, item injection, direct technology unlocks, instant construction, direct production-buffer writes, save editing, and game-speed changes. No pre-existing save may be enumerated, loaded, or read; integration tests use only worlds created by the current Spherewright process.

## Status

- Gate A — secure local Bridge and MCP status: complete on DSP `0.10.34.28529`.
- Gate B — ordinary non-sandbox world plus structured observation: complete for the current DSP build.
- Gate C — normal movement, harvesting, handcrafting, construction, logistics, configuration, research, refuelling, and explicit owned-world saving: in progress; the newly added refuel/save and research-result acknowledgement paths are built but not yet installed or runtime-verified.
- Gate D — end-to-end first red matrix: not started.

The repository contains an older sandbox basic-line experiment that created buildings instantly and inserted ore. It remains useful only as historical DSP API research, is explicitly excluded from Plugin compilation and the public MCP surface, and must not be used as gameplay or acceptance evidence.

Development and DSP validation currently run on this same computer. A same-LAN dedicated game-validation host is documented as a deferred architecture in [docs/remote-validation.md](./docs/remote-validation.md); it is not part of current M0 implementation.

## Architecture

```text
External Agent / MCP host
        | stdio MCP
        v
Spherewright.Mcp                 net8.0
        | authenticated local Named Pipe
        v
Spherewright.Plugin              net472 / BepInEx 5
        | bounded Unity-main-thread commands
        v
DSP normal gameplay systems
```

`Spherewright.Contracts` and `Spherewright.Bridge.Core` contain game-independent DTOs, framing, plans, idempotency, resource/state fingerprints, action state, and quarantine logic. The Plugin is a thin adapter to the current local `Assembly-CSharp.dll`.

## Safety boundary

- `Safety.AllowWrites=false` by default; prepare remains available while commit is blocked.
- All DSP and Unity reads and writes run on Unity's main thread and leave it only as deep-copied DTOs.
- Every write uses prepare, a short-lived opaque plan, an idempotent commit, and before/after readback.
- A result that cannot be proved as before or expected-after quarantines writes for the session.
- `Experience.AutoAcknowledgeResearchResults=true` dismisses completed-research modals through DSP's native `UIResearchResultWindow.FadeOut()` flow; it does not synthesize mouse or keyboard input.
- Mecha refuelling moves already-owned fuel through DSP's native `Mecha.AutoReplenishFuel` path and proves count plus proliferator-point conservation; it does not inject fuel or energy.
- Idle, empty sorters can receive an item filter through the exact current-version `UIInserterWindow` field/sign update path, with topology, carried-cargo, filter, and sign readback bound to the two-phase action.
- Explicit saves are restricted to the exact high-entropy save identity created and owned by the current Plugin process; Spherewright never enumerates or opens other saves.
- Ordinary-mode actions must pay real inventory, energy, distance, technology, construction, and game-time costs.
- Existing save directories and save lists remain out of scope.

See [AGENTS.md](./AGENTS.md), [docs/m0-status.md](./docs/m0-status.md), [docs/handoff-next-computer-agent.md](./docs/handoff-next-computer-agent.md), [docs/red-matrix-capability-audit.md](./docs/red-matrix-capability-audit.md), and [docs/manual-test-m0.md](./docs/manual-test-m0.md) for the authoritative scope, handoff state, and acceptance rules.

## Build

Core and MCP do not require game assemblies:

```powershell
dotnet restore Spherewright.Core.slnf --locked-mode
dotnet build Spherewright.Core.slnf --no-restore
dotnet test Spherewright.Core.slnf --no-build
```

To build the BepInEx Plugin, first copy the minimal local compile references without modifying the game installation:

```powershell
./scripts/sync-game-refs.ps1
dotnet build Spherewright.sln --no-restore
```

License: not selected yet.
