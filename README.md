# Spherewright

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D4)](#requirements)
[![Windows Core CI](https://github.com/AvaloNero/Spherewright/actions/workflows/windows-core-ci.yml/badge.svg)](https://github.com/AvaloNero/Spherewright/actions/workflows/windows-core-ci.yml)

Spherewright is a structured, safety-first control bridge for **Dyson Sphere Program**. It lets an external MCP-capable Agent observe the live game and perform bounded actions through normal DSP systems—without embedding an LLM, editing saves, injecting items, or driving the UI with screenshots and keyboard/mouse macros.

The project is experimental and under active development. The original **M0 — First Red Matrix** milestone is complete; the current development save has also validated automatic power-engine, plastic, titanium-ingot, titanium-alloy, diamond, gear, electric-motor, water, organic-crystal, titanium-crystal, structure-matrix, particle-container, logistics-drone, and planetary-logistics-station production plus same-star interplanetary flight. Two normally built, powered, and complementary planetary logistics stations completed a real 100-titanium local drone shipment, and two normally built interstellar stations completed real vessel delivery of both titanium ore and silicon ore from planet `102` to the home planet. The home station's titanium and silicon outputs are physically connected to production, the temporary stone-to-silicon input is safely disabled, and a sustained structure-matrix run consumed locally automated plastic, refined oil, and water without Icarus cargo. These capabilities shipped in [Spherewright v0.3.0](https://github.com/AvaloNero/Spherewright/releases/tag/v0.3.0). A minimal [v0.3.1 prerelease](https://github.com/AvaloNero/Spherewright/releases/tag/v0.3.1) adds the explicit conversation-confirmed import of a manually loaded save as a new owned copy; cross-computer live validation is still pending. The current development target is **v0.4.0 — Overseer**, a read-oriented diagnostic layer for multi-planet production, power, logistics, and research.

Runtime evidence currently targets DSP `0.10.34.28529`, single-player peaceful mode, sandbox disabled, and 1× resources.

## What it provides

- An authenticated, current-user-only Named Pipe between the game Plugin and the local MCP server.
- Structured reads for the player, progression, recipes, build catalog, resources, factory entities—including detailed logistics-station state—power, the local star system, actions, the per-save gameplay journal, a bounded v0.4 multi-planet native production window with independently recomputed theoretical capacity/utilization, cursor-stable per-planet power/logistics plus global-research summaries, and a versioned same-tick diagnostic bundle that joins those public domains without save identities, paths, or write credentials.
- A directly discoverable MCP Agent playbook resource for opening movement, terminal polling, and bounded recovery from landing-capsule or factory collisions; the same concise file is included in release packages as `AGENT-PLAYBOOK.md`.
- Two-phase `prepare → commit` actions for movement, harvesting, handcrafting, research, construction, building configuration—including no-inventory-mutation logistics-station storage and output-belt selection—player/storage and conservation-checked station-fleet transfers, refuelling, saving, and recovery.
- Native-tick same-star flight with a separately saved, expiring pre-flight checkpoint that remains reusable only while that exact flight needs recovery, then loses its capability on success and retires after the covering primary save.
- Exact owned-world restart handoff: healthy planned restarts load only the ticket-bound primary save, while quarantine recovery alone may use a fresh fixed LastExit whose header already proves the minimum tick. Spherewright never exposes a save picker or enumerates unrelated saves.
- Explicit handoff for a player-loaded save: Spherewright first prepares an exact-session, no-game-side-effect plan and the Agent then asks for confirmation in the conversation. Only a subsequent clear approval may create a separately named owned copy; the original is never overwritten, renamed, deleted, or exposed, and journaling starts at the import boundary.
- Per-save first-event journaling for manual output, production-line output, technology selection, and upgrade selection, including wall-clock/in-save time plus the durable-through sequence, pending-write flag, and persistence error.
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

See [ROADMAP.md](./ROADMAP.md), [docs/protocol.md](./docs/protocol.md), the [save diary index](./docs/save-diaries/README.md), the [incident/fix log](./docs/incident-fix-log.md), and the [experience ledger](./docs/experience-ledger.md) for the approved 0.3–0.9 plan, protocol, per-save history, engineering fixes, and accumulated operational evidence.

## Requirements

The currently supported runtime scope is deliberately narrow:

- Windows x64
- Dyson Sphere Program (the currently validated build is `0.10.34.28529`)
- BepInEx `5.4.17.0`
- single-player
- peaceful mode
- sandbox disabled
- 1× resources

Spherewright does not currently guarantee Dark Fog/combat, sandbox, multiplayer or Nebula, non-1× resources, broad third-party Mod compatibility, an arbitrary save picker, or loading an arbitrary caller-supplied save name.

The versioned Windows release package includes a self-contained MCP server; using it does not require the repository, source code, or a .NET SDK. See [release installation](./docs/release-installation.md).

Source builds additionally need:

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

To produce both supported distribution formats and their SHA-256 sidecars from a clean worktree:

```powershell
./scripts/package-release.ps1 -Version 0.4.0
```

The command creates `Spherewright-<version>-win-x64.zip` for manual installation and `Spherewright-<version>-thunderstore.zip` for Thunderstore/r2modman. The manual package contains the full self-contained MCP directory and installer; the Mod package uses a single-file MCP executable so BepInEx does not scan its runtime dependencies as Plugins. Both archives carry Spherewright integrity metadata and are written under the ignored `artifacts/` directory. The Thunderstore namespace is `Arcueid_77-Spherewright`.

The manual package keeps its extracted-file and MCP handshake smoke test. The Thunderstore package is checked for its required root files, manifest/dependency metadata, 256×256 icon, exact payload hashes, and forbidden bundled game/loader assemblies; runtime installation is then validated in a separate Mod Manager profile or another computer. Creating artifacts does not create a tag, GitHub Release, or Thunderstore version; publication remains gated by [ROADMAP.md](./ROADMAP.md).

To repeat the package integrity and self-contained MCP `initialize`/`tools/list` smoke test independently:

```powershell
./scripts/test-release-package.ps1 -PackagePath ./artifacts/Spherewright-0.4.0-win-x64.zip
./scripts/test-thunderstore-package.ps1 -PackagePath ./artifacts/Spherewright-0.4.0-thunderstore.zip -ExpectedVersion 0.4.0
```

## Local setup

1. Copy `Spherewright.Plugin.dll` into a dedicated folder under DSP's `BepInEx/plugins` directory.
2. Launch DSP once so BepInEx creates `BepInEx/config/dev.spherewright.bridge.cfg`.
3. Keep `Safety.AllowWrites=false` for observation-only use. Set it to `true` only when you intend to authorize structured gameplay commits. To hand a manually loaded save to the Agent, also set `Safety.AllowUserSaveImport=true`; the default is `false`, and import still requires a fresh prepare followed by your explicit confirmation in the conversation.
4. Start the MCP server from the repository:

   ```powershell
   dotnet run --project src/Spherewright.Mcp/Spherewright.Mcp.csproj
   ```

5. Register that stdio command with your MCP host.

Runtime descriptors and credentials are protected for the current Windows user and rotate when the Plugin starts. Do not copy them into logs, issues, or configuration files.

## Quick start

### Start a new world

Leave DSP at its idle main menu, set `Safety.AllowWrites=true`, restart DSP, and ask the Agent to create a new world. Spherewright uses DSP's normal peaceful, non-sandbox, 1× new-game flow and saves it as `Spherewright_New_*`. Before the first gameplay action, the Agent should read MCP resource `spherewright://agent/playbooks/opening-movement-v1`; it explains how to leave the landing capsule without replaying a stalled target. Existing `Spherewright_M0_*` worlds keep their original names and remain eligible for their exact protected resume tickets; Spherewright does not migrate or rename them.

### Continue an existing save

Set both `Safety.AllowWrites=true` and `Safety.AllowUserSaveImport=true`, restart DSP, and manually load the intended peaceful, non-sandbox, single-player, 1× save. Ask the Agent to prepare an import. It must show the returned disclosure and wait for a later explicit confirmation from you before commit creates a separate `Spherewright_Imported_*` copy. The original save is not overwritten, renamed, deleted, or selected by the import API. From then on, both you and the Agent should continue in that copy; after restart, leave DSP at the main menu and use protected resume. After any manual play in the owned copy, the Agent must discard stale observations and plans, read the live state again, and prepare later writes against the current state hashes.

The prefixes are labels, not ownership proofs. A manually loaded save is restricted even if its name looks like a Spherewright name; ownership requires the exact armed new-game transition, a confirmed imported-copy Header proof, or an exact protected resume ticket. An imported save receives a new journal whose coverage begins at the import point and does not invent earlier first-time events.

Repository evidence distinguishes offline build/test and package checks from local live and cross-computer live validation. The save-import path currently has offline implementation/package evidence only in the checked-in record; local end-to-end and cross-computer live import validation remain open and are not implied by these instructions.

## Development status

The release gates live in [ROADMAP.md](./ROADMAP.md). The current save's complete decision, research, upgrade, and first-output chronology lives in its [save diary](./docs/gameplay-timeline.md), indexed with every owned save in [docs/save-diaries/](./docs/save-diaries/README.md). The short version:

- secure local Bridge and MCP surface: complete;
- ordinary peaceful 1× owned-world observation and action primitives: complete for the validated DSP build;
- first automatic red matrix: complete;
- automatic power engine, plastic, titanium ingot, diamond, gear, electric motor, water, organic crystal, titanium crystal, structure matrix, electromagnetic turbine, high-purity silicon, microcrystalline component, sulfuric acid, processor, graphene, thruster, particle container, logistics drone, and planetary logistics station production: complete;
- native same-star checkpointed flight: complete for the validated route;
- planetary/interstellar logistics: released in v0.3.0 after clean-install, protected-resume, live-Bridge, installed-MCP, and same-save regression;
- Overseer multi-planet diagnostics: the v0.4.0 release candidate is ready for owner review. The live implementation pages every already-created owned factory, combines native 600-tick production/consumption rates, runtime-derived theoretical capacity/utilization, per-planet power/logistics, global research, bounded direct and recursive root causes, and a `public_allowlist_v1` same-tick diagnostic bundle. It follows exact item-admitting belt/sorter/splitter and logistics-station routes across unloaded factories, persists logistics progress per protected owned save, and exposes no raw save identity, path, auth token, or write credential. Real shipments covered dispatch, 2,100+ moving ticks without a false stall, pickup, delivery, restored `12 min⁻¹` titanium-ingot production, and two active-route save/normal-exit/exact-resume cycles that excluded offline wall time. Reversible live trials distinguished and repaired `logistics_blocked`, `material_shortage`, and `insufficient_power`; the power trial reached about 58.15% service before normal restoration to ratio 1. Clean packaging, manifest verification, installed Plugin/MCP validation, cursor/privacy checks, exact same-save resume, full world-state audit, and Windows CI have passed; no `v0.4.0` tag or Release is created until the owner approves the candidate evidence and notes. A true 600-game-tick frozen-carrier trial remains an explicit live-coverage limitation: the validated DSP build has no safe normal-game control that freezes an already-dispatched carrier while preserving the same order and route, so Spherewright does not fabricate it through direct runtime-field writes.

There are no stability or compatibility guarantees yet. Before reporting a bug, include the DSP version, BepInEx version, Spherewright commit, the structured error code, and sanitized action/state evidence—never auth tokens, plan tokens, raw save identities, or save files.

## Contributing

Spherewright is currently a personal project and does not accept pull requests before `1.0.0`. Hands-on testers are welcome to open Issues for reproducible problems. See [CONTRIBUTING.md](./CONTRIBUTING.md) for the evidence and privacy guidelines.

## License

Spherewright is available under the [MIT License](./LICENSE).
