# Environment research

Audit date: 2026-08-31 (Asia/Singapore)

| Item | Evidence |
|---|---|
| OS | Windows 10 build 26200, win-x64 |
| .NET SDK | portable 8.0.424 on the current takeover machine; previous verified baseline 9.0.315 |
| .NET 8 runtime | 8.0.28 available |
| DSP location source | Steam auto-detected |
| DSP root | `<DSP_ROOT>` = Steam library `steamapps/common/Dyson Sphere Program` |
| DSP version | 0.10.34.28529 from `Updates/Versions.txt`; Steam build ID 23109513 |
| Assembly-CSharp SHA-256 | `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85` |
| Unity executable version | 2022.3.62.1451004 |
| BepInEx | 5.4.17.0, installed from `xiaoye97-BepInEx-5.4.17` |
| BepInEx.dll SHA-256 | `DC1CB6B58B962BDA5AAA1D6B5F9AE14EC174F61836A1A1F96C1A040C7E8381F7` |
| MCP SDK | `ModelContextProtocol` 2.2.0, stable NuGet package, repository commit `6fa3825973949a9c4f0cd8af344e15a8db09dc35` |
| Plugin target | net472 / BepInEx 5 |
| Core targets | netstandard2.0 |
| MCP/tests target | net8.0 |

Package versions are pinned in `Directory.Packages.props` and resolved transitively in repository `packages.lock.json` files. Game assemblies remain under `.local/game-refs` and are not committed.

## Local paths and commands

No absolute game path is embedded in a project file. Scripts resolve the installation from `SPHEREWRIGHT_DSP_DIR`, Steam registry/library metadata, or an explicit `-DspDir` argument. Documentation uses these variables:

```text
<DSP_ROOT>
<DSP_ROOT>\DSPGAME_Data\Managed\Assembly-CSharp.dll
<DSP_ROOT>\BepInEx\plugins\Spherewright
```

The local development flow used:

```powershell
./scripts/locate-dsp.ps1 -AsJson
./scripts/sync-game-refs.ps1
dotnet restore Spherewright.Core.slnf --locked-mode
dotnet build Spherewright.sln --no-restore
dotnet test Spherewright.sln --no-build --no-restore
./scripts/install-dev-plugin.ps1 -NoBuild
./scripts/smoke-test.ps1 -LiveBridge
```

`sync-game-refs.ps1` copied only the required compile references to `.local/game-refs` and recorded hashes; it did not change a game assembly. `install-dev-plugin.ps1` copied only Spherewright outputs plus its JSON dependency to `<DSP_ROOT>\BepInEx\plugins\Spherewright` and refuses to run while DSP is active.

## Current verification result

- Full solution build: succeeded with 0 warnings and 0 errors.
- Automated tests after checkpoint lifecycle, resume-source, movement recovery, belt free-end, and journal durability changes: 76 passed (Contracts 4, Bridge.Core 59, MCP 13).
- BepInEx load: verified in DSP at the main menu and in a current-process-created world.
- MCP Inspector: the 44-tool surface compiles and is covered by registration tests. The current owned world runtime-verified explicit save, research-result acknowledgement, normal refuel, exact-proof quarantine reconciliation, planned exact-primary resume, both refinery-output sorter filters, journal durable-through readback, and legacy checkpoint retirement. New-flight lifecycle/stalled readback and quarantine-only LastExit selection remain the live gaps.
- Historical sandbox basic line: six entities built and reread; the installed DLL converted 6 iron ore into 6 iron ingots under wind power, following an earlier 20-to-20 run. This is not evidence for revised M0 Gates B-D.
- Revised ordinary mode: live verification created a peaceful 1x non-sandbox owned world and exercised structured research plus normal drone-built power, copper/iron/coal/oil extraction, logistics, smelting, electromagnetic-matrix research, graphite, steel, oil refining, co-product separation, and energy-matrix production. The earlier co-located-sorter attribution failure was repaired and live-validated after strict same-world recovery; lab `256` then produced energy matrices `0 -> 3 -> 6`, and the exact owned world was saved normally. The same live lab later accumulated 10 before its output buffer filled.

## Takeover-machine audit

The current checkout was re-audited on 2026-08-30 and the live milestone evidence was refreshed on 2026-08-31:

- The system-wide `dotnet` installation contained only runtime 8.0.7 and no SDK. SDK 8.0.424 was therefore installed under ignored `.local/dotnet` with Microsoft's official `dotnet-install.ps1`; no system directory or global `PATH` was changed.
- The user NuGet configuration contained no package sources. Locked restore succeeded by supplying the official `https://api.nuget.org/v3/index.json` source on the command line; the user configuration and repository lock files were not changed.
- The .NET SDK's environment-dependent automatic `Microsoft.NETFramework.ReferenceAssemblies` umbrella reference conflicted with the Plugin's locked, explicit `.net472` package. The Plugin project now disables that implicit injection, so full-solution locked restore is reproducible without changing `packages.lock.json`.
- Steam auto-detection initially returned the same DSP root twice with different path casing. `locate-dsp.ps1` now de-duplicates validated roots with Windows case-insensitive path semantics.
- Steam build ID remains `23109513`; `Assembly-CSharp.dll` remains SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`, matching the previously researched current-version API baseline.
- After explicit user approval, official `xiaoye97-BepInEx-5.4.17` was installed and its `BepInEx.dll` hash matched the recorded baseline. The development Plugin loaded successfully and created exactly one user-authorized validation world.
- The full solution builds with 0 warnings and 0 errors. The current suite passes 76 tests: Contracts 4, Bridge.Core 59, MCP 13.
- The installed Plugin's earlier positional build readback selected an older sorter when two refinery outputs shared an exact source pose. Source now snapshots and excludes pre-existing co-located sorter IDs; the `211`/`213` regression is covered offline, and the corrected DLL was later deployed through the strict recovery path and live-validated with distinct co-located outputs `164` and `181` in the current healthy session.
- Source now includes a pure movement-progress watchdog and player-order single-flight. Five Core tests cover the watchdog, and the full Plugin compiles against the current game DLL; this latest movement change is intentionally not hot-deployed into the still-running healthy process.
