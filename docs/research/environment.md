# Environment research

Audit date: 2026-08-30 (Asia/Shanghai)

| Item | Evidence |
|---|---|
| OS | Windows 10 build 26200, win-x64 |
| .NET SDK | 9.0.315 |
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
- Automated tests after the latest M0 scope revision: 38 passed (Contracts 3, Bridge.Core 26, MCP 9).
- BepInEx load: verified in DSP at the main menu and in a current-process-created world.
- MCP Inspector: the earlier safe surface was verified; the current 34-tool surface compiles and is covered by registration tests, but the newly added refuel, explicit-save, sorter-filter, and research-result acknowledgement paths require revalidation after rebuilding/installing.
- Historical sandbox basic line: six entities built and reread; the installed DLL converted 6 iron ore into 6 iron ingots under wind power, following an earlier 20-to-20 run. This is not evidence for revised M0 Gates B-D.
- Revised ordinary mode: live verification created a peaceful 1x non-sandbox owned world and exercised structured research plus normal drone-built power, mining, logistics, and iron smelting. The composite sandbox line is absent from MCP registration and excluded from the current Plugin binary. The uninterrupted first-red-matrix run remains pending.
