# Spherewright

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D4)](#requirements)

Spherewright is a structured, safety-first control bridge for **Dyson Sphere Program**. It lets an external MCP-capable Agent observe the live game and perform bounded actions through normal DSP systems—without embedding an LLM, editing saves, injecting items, or driving the UI with screenshots and keyboard/mouse macros.

The project is experimental and under active development. The original **M0 — First Red Matrix** milestone is complete; the current development save has also validated automatic power-engine, plastic, titanium-ingot, diamond, gear, electric-motor, and water production plus same-star interplanetary flight. Work is now advancing through organic and titanium crystals toward the yellow-matrix production chain.

Runtime evidence currently targets DSP `0.10.34.28529`, single-player peaceful mode, sandbox disabled, and 1× resources.

## What it provides

- An authenticated, current-user-only Named Pipe between the game Plugin and the local MCP server.
- Structured reads for the player, progression, recipes, build catalog, resources, factory entities, power, the local star system, actions, and the per-save gameplay journal.
- Two-phase `prepare → commit` actions for movement, harvesting, handcrafting, research, construction, building configuration, transfers, refuelling, saving, and recovery.
- Native-tick same-star flight with a separately saved, reusable pre-flight checkpoint.
- Exact owned-world restart handoff through DSP's fresh fixed LastExit flow, with a ticket-bound latest-healthy-primary fallback only when a failed shutdown did not refresh LastExit; Spherewright never exposes a save picker or enumerates unrelated saves.
- Per-save first-event journaling for manual output, production-line output, technology selection, and upgrade selection, including wall-clock and in-save time.
- Readback, state hashes, short-lived plans, idempotency, single-flight execution, and write quarantine when a result cannot be proved.

Spherewright is a control layer, not an autonomous planner. The external Agent decides what to do; Spherewright supplies typed state, legal primitives, and evidence-backed results.

## Architecture

```text
External Agent / MCP host
        │ stdio MCP
        ▼
Spherewright.Mcp                 .NET 8
        │ authenticated local Named Pipe
        ▼
Spherewright.Plugin              .NET Framework 4.7.2 / BepInEx 5
        │ bounded Unity-main-thread work
        ▼
DSP native gameplay systems
```

- `Spherewright.Contracts` contains the public DTOs and protocol contracts.
- `Spherewright.Bridge.Core` contains game-independent framing, safety, plan, fingerprint, and idempotency logic.
- `Spherewright.Plugin` is the thin adapter to the current DSP/Unity runtime.
- `Spherewright.Mcp` exposes the Bridge as MCP tools over stdio.

## Safety model

Writes are disabled by default. When enabled, every gameplay mutation is bound to a current owned session and follows a fresh read, a non-mutating prepare, one idempotent commit, and terminal/readback verification.

Spherewright deliberately does not use:

- sandbox mode, item injection, direct buffer writes, instant construction, technology injection, or game-speed changes;
- save editing, save enumeration, or loading an arbitrary save name;
- external memory scanning or modifications to `Assembly-CSharp.dll`;
- Computer Use, visual recognition, or keyboard/mouse macros for game operations.

All DSP and Unity access runs on Unity's main thread. Only deep-copied DTOs leave that thread. Ambiguous write outcomes quarantine further commits until the exact retained action can be proved or the same owned world is safely restarted from protected evidence.

See [docs/protocol.md](./docs/protocol.md), [docs/m0-status.md](./docs/m0-status.md), and [docs/experience-ledger.md](./docs/experience-ledger.md) for the protocol, live validation status, and accumulated operational evidence.

## Requirements

- Windows
- Dyson Sphere Program (the currently validated build is `0.10.34.28529`)
- BepInEx 5 installed in DSP
- .NET 8 SDK
- PowerShell 7 recommended for the helper scripts

No game assemblies are committed to this repository. They remain local and are copied only into the ignored `.local/game-refs` build directory.

## Build and test

Core contracts, Bridge logic, and MCP tests do not require game assemblies:

```powershell
dotnet restore Spherewright.Core.slnf --locked-mode
dotnet build Spherewright.Core.slnf --no-restore
dotnet test Spherewright.Core.slnf --no-build
```

To build the BepInEx Plugin, first sync the minimal compile references from your local DSP/BepInEx installation, then build the full solution:

```powershell
./scripts/sync-game-refs.ps1
dotnet build Spherewright.sln --no-restore
```

If DSP is installed somewhere the locator cannot find automatically:

```powershell
./scripts/sync-game-refs.ps1 -DspDir 'D:\Games\Dyson Sphere Program'
```

The Plugin output is `src/Spherewright.Plugin/bin/Debug/net472/Spherewright.Plugin.dll` by default, or the corresponding `Release` directory when built with `--configuration Release`.

## Local setup

1. Copy `Spherewright.Plugin.dll` into a dedicated folder under DSP's `BepInEx/plugins` directory.
2. Launch DSP once so BepInEx creates `BepInEx/config/dev.spherewright.bridge.cfg`.
3. Keep `Safety.AllowWrites=false` for observation-only use. Set it to `true` only when you intend to authorize structured gameplay commits.
4. Start the MCP server from the repository:

   ```powershell
   dotnet run --project src/Spherewright.Mcp/Spherewright.Mcp.csproj
   ```

5. Register that stdio command with your MCP host.

Runtime descriptors and credentials are protected for the current Windows user and rotate when the Plugin starts. Do not copy them into logs, issues, or configuration files.

## Development status

The detailed, evidence-backed status lives in [docs/m0-status.md](./docs/m0-status.md). The short version:

- secure local Bridge and MCP surface: complete;
- ordinary peaceful 1× owned-world observation and action primitives: complete for the validated DSP build;
- first automatic red matrix: complete;
- automatic power engine, plastic, titanium ingot, diamond, gear, electric motor, and water: complete;
- native same-star checkpointed flight: complete for the validated route;
- yellow-matrix chain and broader compatibility: in progress.

There are no stability or compatibility guarantees yet. Before reporting a bug, include the DSP version, BepInEx version, Spherewright commit, the structured error code, and sanitized action/state evidence—never auth tokens, plan tokens, raw save identities, or save files.

## Contributing

Issues and focused pull requests are welcome. Read [AGENTS.md](./AGENTS.md) before changing gameplay behavior: it documents the main-thread boundary, ordinary-game constraints, proof requirements, and repository workflow. New DSP calls should be grounded in the exact current game assembly and recorded under `docs/research/`.

## License

Spherewright is available under the [MIT License](./LICENSE).
