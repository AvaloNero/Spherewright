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

`sync-game-refs.ps1` copied only the required compile references to `.local/game-refs` and recorded hashes; it did not change a game assembly. `install-dev-plugin.ps1` copied only Spherewright outputs plus its JSON dependency to `<DSP_ROOT>\BepInEx\plugins\Spherewright` and refuses to run while DSP is active. On 2026-09-02, after normal save tick `9413535` and normal process exit, the 46-tool Release assembly set was deployed together and matched its build outputs byte-for-byte: Plugin `36E164835BDC8C3F208FF9C98DD0138148B260923FC3602A8ECD83A735D9E003`, Contracts `76F8AF903BEA5D15067D46B766D33F5F79BA034F922C47AE07D5FE90551CD70E`, and Bridge.Core `4D99FC51E549152DBB209BA6D5CAC90F8BB1AED33F4F3CAAB1A5F00695C4397F`. The protected exact-primary resume then succeeded and automatically resaved tick `9413567`; no game assembly or save file was edited.

## Verification history (newest first)

- On 2026-09-03, the third v0.4 Overseer slice completed a source-equal live deployment. The first candidate correctly failed closed on a legal depleted-miner shape, which was then fixed and covered by a zero-source regression test. A normal save at tick `13831872` was followed by an orderly window close; all seven Debug deployment files matched their build outputs, including final Plugin SHA-256 `5AC257D5AB8013E7D088A8609D08A9FA7FD83A633D4D2DA2F0F549BA53815DC1`. Protected exact-primary resume automatically resaved tick `13831903`. The active three-factory world returned complete current-component theoretical rates for matrices, mining, water, oil, smelting, chemical production, and assembly; a three-page snapshot shared tick `13837732`. The source MCP `0.4.0.0` completed `initialize`, listed 50 tools, and called the live production endpoint. The full solution built with 0 warnings/errors and all 160 tests passed (Contracts 17, Core 122, MCP 21).
- On 2026-09-03, the second v0.4 Overseer slice passed a final source-equal deployment after the research queue identity gate was tightened. A normal save at tick `13767062` was followed by an orderly window close; seven Debug deployment files matched their build outputs, including Plugin SHA-256 `3766E3A770FFB7BAA24FA870CA569BD90F5BE776802A04F213EB2634B79E9C6E`. The protected exact-primary resume automatically resaved tick `13767093`. A three-page `limit=1` snapshot shared tick `13773036`, safely returned factories `104/102/103`, accepted the valid `[3401]` queue, preserved a ready native production window, and reported corrected generated/exported energy. The full solution built with 0 warnings/errors and all 150 tests passed (Contracts 17, Core 112, MCP 21).
- On 2026-09-03, the station output-selector batch built with 0 warnings / 0 errors and 114 tests passed (Contracts 13, Bridge.Core 82, MCP 19). After normal save tick `10449537` and an orderly game exit, Release Plugin `941951FA0F5B8ADDEE16EF1B66B0ABA98664BF79474F49AEFACD6668071B0C76`, Contracts `1DD0C244463FBB78EA4C769990341E8699E3372E4F4155693869961B77097274`, and Bridge.Core `F032394C60CC9840FEE854A2ADC3702D7EA942378DBA75D939EE24894999DDF6` matched the deployed files byte-for-byte. Exact-primary resume and live PLS output selection then restored titanium-crystal production; normal save tick `10456408` covers the result.
- Full solution build: succeeded with 0 warnings and 0 errors.
- Automated tests after checkpoint lifecycle, resume-source, movement recovery, belt free-end, and journal durability changes: 76 passed (Contracts 4, Bridge.Core 59, MCP 13).
- BepInEx load: verified in DSP at the main menu and in a current-process-created world.
- MCP Inspector: the 46-tool source surface compiles and is covered by registration tests, including the new two-phase logistics-station fleet transfer. The matching Plugin/Core/Contracts build is now live after a normal save/close/deploy/resume boundary. The current owned world runtime-verified explicit save, research-result acknowledgement, normal refuel, exact-proof quarantine reconciliation, planned exact-primary resume, both refinery-output sorter filters, journal durable-through readback, and legacy checkpoint retirement. Station fleet transfer remains pending first-tower validation, not deployment.
- The first normally built PLS exposed the native local-station `planetId == 0` sentinel. The identity-policy correction builds with 0 warnings / 0 errors and 101 tests pass (Contracts 11, Bridge.Core 73, MCP 17). After normal save tick `9462208`, the corrected Release set was deployed together and matched its outputs: Plugin `0086A97036B19C43178DBC8129EF53CE31E8F68E613178B068B1945B90BAA600`, Contracts `0A244DCA1C72D46079A2B05D5AE67C7A04EF3C52FF059971857080BF7414D579`, and Bridge.Core `058E3EFD37E5EA4E7D63487B19A788E207712D4D37C1205B8B935EF0E3B98BF3`. Exact-primary resume and live entity `916` inspection returned the complete normalized local-station DTO; configuration and fleet commits remain separate pending validations.
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
- The full solution builds with 0 warnings and 0 errors. The current suite passes 160 tests: Contracts 17, Bridge.Core 122, MCP 21.
- The installed Plugin's earlier positional build readback selected an older sorter when two refinery outputs shared an exact source pose. Source now snapshots and excludes pre-existing co-located sorter IDs; the `211`/`213` regression is covered offline, and the corrected DLL was later deployed through the strict recovery path and live-validated with distinct co-located outputs `164` and `181` in the current healthy session.
- Source now includes a pure movement-progress watchdog and player-order single-flight. Five Core tests cover the watchdog, and the full Plugin compiles against the current game DLL; this latest movement change is intentionally not hot-deployed into the still-running healthy process.
