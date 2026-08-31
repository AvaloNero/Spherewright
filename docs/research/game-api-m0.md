# DSP game API research — M0

Assembly evidence:

- DSP version: `0.10.34.28529`.
- `Assembly-CSharp.dll` SHA-256: `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`.
- Inspection tool: ILSpy command line 9.1.0.7988, used only for targeted type/signature inspection. Decompiled game source is not stored in this repository.

## 2026-08-30 M0 scope revision — ordinary new game

M0 now targets the first red matrix under ordinary gameplay and excludes all sandbox/item-injection/instant-build evidence from completion. Targeted inspection of the same assembly confirmed:

```text
public void GameDesc.SetForNewGame(int galaxyAlgo, int galaxySeed,
    int starCount, int playerProto, float resourceMultiplier)
public static void DSPGame.StartGameSkipPrologue(GameDesc gameDesc)
public void GameData.SkipStandardModeGuide()
public void GuideMissionStandardMode.Skip(GameData gameData)
public void GameData.InitLandingPlace()
public void Player.SetForNewGame()
public void GameHistoryData.SetForNewGame()
```

`GameDesc.SetForNewGame` sets `isPeaceMode=true`, `isSandboxMode=false`, initializes achievements for the current creation date, and retains the supplied resource multiplier. The revised coordinator supplies `1f`, explicitly keeps peaceful mode, and explicitly keeps sandbox disabled. `DSPGame.StartGameSkipPrologue` copies `isSandboxMode` only into `WillEnableSandboxTools`; `GameLoader` then invokes `SkipStandardModeGuide` at tick zero.

The inspected skip path flattens only the landing patch, creates the ordinary landing-capsule vegetable, and initializes the player's landing transform. `Player.SetForNewGame` and `GameHistoryData.SetForNewGame` establish the normal configured initial package, recipes, technologies, mecha, and construction parameters before either full or skipped prologue. No item grant or technology unlock was found in `SkipStandardModeGuide` or `InitLandingPlace` themselves. The installed-DLL run later confirmed an empty initial inventory and handcraft queue, `GameMain.sandboxToolsEnabled=false`, peaceful mode, and 1x resources.

The public MCP names have been changed to `spherewright_prepare_new_game` and `spherewright_commit_new_game`. The old composite basic-line MCP registration has been removed; its coordinator remains historical code pending deletion or replacement by ordinary-game primitives.

## Ordinary-world startup readiness and live evidence

An initial live attempt reached `GameMain.Awake` too early and failed in `GPUInstancingManager.Init` with a `NullReferenceException`. Targeted inspection showed that the first access in that method is:

```text
ModelProto[] modelArray = LDB.models.modelArray;
```

The prior gate checked only main-menu visibility and absence of `GameLoader`; those conditions can become true before DSP finishes prototype and model preloading. Current-version `VFPreload` exposes:

```text
public static bool VFPreload.done
public static bool VFPreload.dbDone
```

The preload coroutine sets both only after model, vegetation, vein, item, recipe, technology, effect, tutorial, achievement, and other prototype preload work completes. New-world prepare and commit now require `VFPreload.done`, `VFPreload.dbDone`, and a non-empty `LDB.models.modelArray`; otherwise they fail without mutation as `BRIDGE_NOT_READY`. The live test treats that response, and a main-thread read timeout during the same startup interval, as retryable only for read-only prepare.

A separate startup retry exposed a stale-descriptor race where `Process.ProcessName` can throw after `GetProcessById` succeeds but the old process exits. The publisher now treats `ArgumentException`, `InvalidOperationException`, and `Win32Exception` as a dead process and removes the stale descriptor before publishing the new one.

After both fixes, action `4cc929fb-c5c0-4e59-96e6-e9cb3c5940a8` completed in a fresh process-owned world. Session `3d84bb3d-1e15-497f-901a-3bf8375490f1` on planet `103` reread peaceful mode, sandbox disabled, resource multiplier `1.0`, player state, 314 technology states, 161 recipes, 174 items, resource nodes, empty factory/power state, and runtime red-matrix dependency data. Same-key new-game replay and cross-filter cursor rejection also passed. No pre-existing save was enumerated, loaded, or read.

## Gate A verified symbols

Targeted inspection of `GameConfig` found:

```text
public static Version GameConfig.gameVersion { get; set; }
public static int GameConfig.build
public static string GameConfig.versionFilename = "Updates/Versions.txt"

[Serializable] public struct Version
public int Version.Major
public int Version.Minor
public int Version.Release
public int Version.Build
public string Version.ToFullString()
```

Gate A uses these values only from the Unity main thread to produce an immutable game-version string for bridge status. It does not access `GameMain`, `GameData`, save metadata, factories, or Unity objects from the Pipe worker.

## Gate A runtime evidence

- The Unity Mono runtime does not implement `WindowsIdentity.User`; the Plugin therefore gets the current SID from the Windows process token and creates the directory, descriptor, and Named Pipe with native protected DACLs.
- Live descriptor ACL: owner `<CURRENT_WINDOWS_USER>`, inheritance disabled, one current-user `FullControl` rule.
- An invalid token was rejected, a subsequent valid connection succeeded, and MCP Inspector called `spherewright_get_status` through stdio.
- Normal main-menu shutdown removed the runtime descriptor and produced no unhandled Plugin exception.

## Session and peaceful-mode symbols

The current assembly exposes these relevant global-namespace members:

```text
public static GameData GameMain.data
public static long GameMain.gameTick
public static Player GameMain.mainPlayer
public static bool GameMain.sandboxToolsEnabled
public static void DSPGame.StartGameSkipPrologue(GameDesc _gameDesc)
public static bool GameSave.SaveCurrentGame(string saveName)
public PlanetData GameData.localPlanet { get; private set; }
public PlanetFactory GameData.localLoadedPlanetFactory { get; }
public bool GameDesc.isPeaceMode
public bool GameDesc.isCombatMode => !isPeaceMode
```

Historical implementation note: the original `TestWorldCoordinator` created a peaceful sandbox world. The revised coordinator now creates a peaceful 1x non-sandbox world and preserves the `SetForNewGame` achievement default; it still uses `DSPGame.StartGameSkipPrologue` and exact next-`GameData` ownership. `GameSessionTracker` never adopts by save-name pattern, and ownership is discarded at the menu or Plugin shutdown. An unowned `GameData` produces restricted status and blocks all save/factory reads.

For this game version, `GameDesc.isPeaceMode` is the reliable black-fog boundary because `isCombatMode` is its inverse. A missing descriptor is treated as unknown and blocks writes. Owned worlds are saved only with `GameSave.SaveCurrentGame` after the local factory exists; save files are never opened or modified directly.

## Factory and assembler read path

The adopted session's local factory is obtained through `GameMain.data.localLoadedPlanetFactory`. Assembler snapshots use:

```text
PlanetFactory.factorySystem
public AssemblerComponent[] FactorySystem.assemblerPool
public int FactorySystem.assemblerCursor
PlanetFactory.entityPool[assembler.entityId]
EntityData.assemblerId
AssemblerComponent.recipeId
AssemblerComponent.recipeExecuteData
AssemblerComponent.replicating
```

Both entity and component IDs are cross-checked before a DTO is copied. Recipe/building names are resolved through `LDB.recipes.Select` and `LDB.items.Select`; no DSP pool or Unity object leaves the main thread. Listing is capped at 100 and the opaque cursor binds both session and revision.

The live build catalog enumerates `LDB.items.dataArray` and `LDB.recipes.dataArray`, filters on current `PrefabDesc` flags and `ERecipeType`, and uses `GameHistoryData.ItemUnlocked(int)` / `RecipeUnlocked(int)`. No prototype ID is hard-coded into the action.

## Research-result acknowledgement UI

The current assembly exposes the same path used by the visible confirm button and Escape handling:

```text
public static UIRoot UIRoot.instance { get; }
public UIGame UIRoot.uiGame
public UIResearchResultWindow UIGame.researchResultTip
public bool ManualBehaviour.active { get; }
public bool UIResearchResultWindow.ready { get; }
public void UIResearchResultWindow.FadeOut()
protected override void UIResearchResultWindow._OnClose()
public void GameScenarioLogic.NotifyTechResult(int techId)
```

`UIGame` calls `FadeOut()` for both an accepted Escape input and its ordinary close handling. `ready` is exactly `contentGroup.alpha == 1f`; the guarded `FadeOut()` assignment changes that alpha to `0.999f` immediately and sets `windowHeight=0`. Consequently the next Plugin frame no longer satisfies `ready`, so Spherewright cannot repeatedly acknowledge the same result while DSP's normal update animates it closed. That update subsequently calls `_Close()`, whose `_OnClose()` clears the UI state and notifies `GameScenarioLogic` of the displayed technology. Spherewright checks this UI only from `Plugin.Update()` and does not synthesize a key, mouse event, screen coordinate, or Computer Use action. The local `Experience.AutoAcknowledgeResearchResults` setting defaults to `true` and can disable this presentation-only behavior without affecting research state.

## Mecha refuel and explicit owned-save path

Targeted current-assembly inspection confirmed:

```text
public bool Mecha.AutoReplenishFuel(int itemId, int grid)
public int StorageComponent.GetItemCount(int itemId, out int inc)
public static int[] StorageComponent.itemStackCount
public static bool[] StorageComponent.itemIsFuel
public static bool GameSave.SaveCurrentGame(string saveName)
```

`Mecha.AutoReplenishFuel` takes at most one native item stack from `Player.package`, inserts it into exactly the supplied reactor-storage grid through the fuel-typed `StorageComponent.AddItem` overload, and returns any unaccepted remainder to the package. Spherewright's prepare reproduces the current grid/filter/stack-capacity decision without mutation and binds package plus fuel-storage contents in the player-action fingerprint. Commit calls the native method once and rereads both containers; the expected package decrement, fuel-storage increment, combined count, and combined proliferator points must all match. It never writes `coreEnergy`, `reactorEnergy`, a storage grid, or an item count directly.

The explicit save action binds the active owned session, local planet, current revision, and the high-entropy save name retained internally when Spherewright created that world. Commit calls `GameSave.SaveCurrentGame` only with that exact identity and records `lastOwnedSaveGameTick` after a true return. The path does not enumerate a save directory, discover save names, open a save, or accept a client-provided save name. It is compile/Core/MCP covered and runtime-validated repeatedly in the current owned world, including the M0 acceptance save at tick `2499658` and the later continuation save at tick `2710106`.

## Sorter item-filter path

Plasma refining has multiple products, so a first-red-matrix line cannot treat an unfiltered refinery output as deterministic. Targeted current-assembly inspection found no public `FactorySystem` filter setter. The actual sorter window handlers use this exact path:

```text
private void UIInserterWindow.OnItemPickerReturn(ItemProto item)
private void UIInserterWindow.OnResetFilterButtonClick()
public int InserterComponent.filter
public int InserterComponent.itemId
public int InserterComponent.stackCount
public short InserterComponent.itemCount
public short InserterComponent.itemInc
public EInserterStage InserterComponent.stage
public uint SignData.iconId0
public uint SignData.iconType
```

`OnItemPickerReturn` assigns `item.ID` to `InserterComponent.filter`, then assigns the same ID to the sorter's entity sign and sets `iconType=1`; reset writes zero to all three values. Because the current version exposes no safer business setter, the Plugin adapter reproduces only those exact assignments. It first requires the precise built sorter/component identity, both connection targets, `stage=Picking`, zero time, and no carried item/count/stack/inc. The factory snapshot and canonical hash include sorter endpoints, filter, stage, stack count, and held-cargo buffers. Commit repeats all checks, performs the UI-equivalent assignments once, then rereads the component filter and sign. Candidate session `73b4019b-c5cc-4f90-b1f4-bc4abc6d49c6` first runtime-verified this path on sorter `211` with refined-oil filter `1114`; the strictly recovered current world independently validated refined-oil sorter `164` and hydrogen sorter `181`, including carried cargo and distinct destinations.

## Runtime dependency graph correction

The first-red-matrix graph is pure catalog logic and now lives in `Spherewright.Bridge.Core.Progression.RuntimeDependencyGraphBuilder`, not the game adapter. The former traversal incorrectly marked every recipe output as an already visited item. When a hydrogen-producing alternative also output energetic graphite, traversal order could suppress the independent coal-to-graphite producer. The corrected traversal marks an item visited only when it is actually popped for dependency expansion; recipe outputs add edges but do not suppress pending inputs. A Core test covers red matrix, plasma refining, X-ray cracking co-products, and coal smelting and proves that the coal input/recipe remains in the graph.

## Build and connection path

Targeted ILSpy inspection confirmed:

```text
public bool BuildTool_Click.CheckBuildConditions()
public void BuildTool_Click.CreatePrebuilds()
protected StorageComponent BuildTool.tmpPackage
public void PlanetFactory.WriteObjectConn(int objId, int slot, bool isOutput,
    int otherObjId, int otherSlot)
public void FactorySystem.SetInserterPickTarget(int inserterId, int pickTarget, int offset)
public void FactorySystem.SetInserterInsertTarget(int inserterId, int insertTarget, int offset)
```

`BuildTool_Click.CheckBuildConditions` evaluates a copied `tmpPackage`. The ordinary action's isolated click/path/inserter tools copy the real player package and require the requested owned building items to already exist; prepare releases the copy and never touches player inventory or the factory.

Commit repeats the exact validation and invokes `CreatePrebuilds` once. It requires ordinary negative prebuild IDs and proves that exactly the planned owned items were consumed. Spherewright does **not** call `PlanetFactory.BuildFinally`; construction drones and normal game ticks must replace every prebuild. Completion then resolves matching built entities and rereads resource-node binding, belt topology, or sorter pick/insert targets as applicable. An unprovable disappearing or timed-out prebuild quarantines writes instead of performing a guessed rollback.

DSP anchors multiple sorters leaving the same building at the same `entity.pos`. In the earlier candidate, sorter `211` connected refinery `203` to storage `210`; the later legal sorter `213` connected the same refinery to storage `212` at the exact same pose. The installed positional resolver selected lower, older ID `211`, topology verification observed `203 -> 210` instead of `203 -> 212`, and the write subsystem quarantined. Entity `213` and the one-item construction decrement were both visible afterward, so retrying would have been unsafe. The corrected resolver snapshots pre-existing co-located sorter IDs immediately before `CreatePrebuilds` and excludes them during completion attribution. Core tests reproduce the `211`/`213` tie and prove selection of the new ID. After deployment through the strict same-world recovery path, old refined-oil sorter `164` and new hydrogen sorter `181` shared the source pose but were attributed separately and reached their intended destinations without quarantine.

## Recipe, storage, and power path

The current signatures are:

```text
public void AssemblerComponent.SetRecipe(int recpId, SignData[] signPool)
public void GameScenarioLogic.NotifyOnAssemblerRecipePick(
    int factoryId, int assemblerId, int recipeId,
    int[] items, int[] itemCounts, int[] products, int[] productCounts)
public int StorageComponent.TakeItem(int itemId, int count, out int inc)
public int StorageComponent.AddItemStacked(
    int itemId, int count, int inc, out int remainInc)
public int StorageComponent.GetItemCount(int itemId)
public PowerConsumerComponent[] PowerSystem.consumerPool
public int PowerSystem.consumerCursor
public PowerGeneratorComponent[] PowerSystem.genPool
public int PowerSystem.genCursor
```

`UIAssemblerWindow` uses `AssemblerComponent.SetRecipe`, immediately passes `recipeExecuteData` arrays to `NotifyOnAssemblerRecipePick`, and registers feature key `1000109`. The ordinary configuration action follows that business path only after proving the exact built device is idle with empty buffers and that the recipe is unlocked and applicable. It rereads the component and execute data and never directly assigns `recipeId` or replaces a pool entry.

Explicit player/storage interaction uses `TakeItem` and `AddItemStacked` only after copy-based capacity proof, proximity proof, and exact target-state binding. Commit rereads equal-and-opposite container deltas and combined conservation; it never uses `PlanetFactory.InsertIntoStorage` in the revised M0 public path. Power observation validates component identities and reports normal power-network supply/consumer/generator state.

## Historical sandbox evidence — excluded from revised M0

In one current-process-owned peaceful sandbox world, the runtime catalog selected storage `2101`, smelter `2302`, sorter `2011`, wind generator `2203`, and smelting recipe `1` (iron ore `1001` to iron ingot `1101`). The dry-run found a clear layout without changing revision or factory state.

Commit action `9170d11c-e458-4d11-b1d1-a406b818d890` created and reread entities 1 through 6, inserted exactly 20 ore, saved successfully, and advanced revision from 1 to 2. An intermediate inspection observed 5 ore / 13 ingots with the assembler working; the completion inspection observed 0 ore / 20 ingots with structure, connection, recipe, and shared power-network checks all true. A same-key retry returned the same action/entity IDs with `idempotentReplay=true` and revision 2. A prepare using revision 1 returned `STALE_REVISION` without mutation.

After the final partial-build rollback hardening, a second fresh current-process world verified the installed final DLL itself. Action `e77fd86a-2e5f-474b-a1a4-f845252be221` again created entities 1 through 6, advanced revision 1 to 2, saved successfully, and converted 6 iron ore into 6 iron ingots. Structure, connection, recipe, and power-network checks were all true, and a same-key retry returned `idempotentReplay=true` without a second mutation.

No pre-existing save was enumerated, loaded, or read during this research or runtime verification. The ordinary non-sandbox world and structured observation path have live runtime verification; normal-play actions and the complete first-red-matrix path do not yet.
