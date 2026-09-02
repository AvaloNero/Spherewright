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

## Per-save first-event journal signals

Targeted metadata and IL inspection of the current `Assembly-CSharp.dll` established these exact signals:

```text
public void MechaForge.GameTick(long time, float deltaTime)
public void ForgeTask.Produce()
public void GameHistoryData.AddFeatureValue(int featureId, int addVal)
public int GameHistoryData.GetFeatureValue(int featureId)
public int[] FactoryProductionStat.productRegister
public void FactoryProductionStat.PrepareTick()
public void FactoryProductionStat.ClearRegisters()
public void FactoryProductionStat.GameTick(long time)
public int TechProto.page { get; }
```

For every completed forge task, `MechaForge.GameTick` first registers its product statistics, calls `ForgeTask.Produce`, then calls `GameHistoryData.AddFeatureValue(2140000 + recipeId, 1)`. This also covers nested handcraft prerequisite tasks, unlike the public `onTaskDelivery` event, which fires only for a top-level task. Spherewright therefore reconstructs cumulative manual item counts from the persisted per-recipe feature counter and each runtime recipe's `Results/ResultCounts`; it does not infer handcrafting from an inventory increase.

Automated factory systems write their current-tick outputs to `FactoryProductionStat.productRegister`; current call sites include miner, assembler, fractionator, lab, transport, power and Dyson production ticks. `FactoryProductionStat.PrepareTick` clears this register at the start of a tick, and `FactoryProductionStat.GameTick` consumes it into production statistics. By contrast, `Mecha.AddProductionStat` calls `AddProductionToTotalArray` directly and does not write `productRegister`. A positive register value observed in the Plugin main-thread update is therefore an independent production-line signal rather than a manual-crafting echo.

`TechProto.page` returns `0` for IDs below `2000` and `1` otherwise, matching DSP's technology/upgrade pages. A first selection is captured from the normal `GameHistoryData.currentTech/techQueue` state; no technology is unlocked or advanced by the journal. Each event stores `DateTimeOffset.Now` as an ISO-8601 actual time and `GameMain.gameTick` both raw and formatted at 60 ticks per in-save second.

Journal files live below the current-user-protected Spherewright runtime directory and are named only by a SHA-256 identity derived from the internally retained owned-save name. New Spherewright worlds get complete prospective coverage from their adoption frame. When this feature first attaches to an already progressed owned save, existing manual, production and research IDs are seeded as historical without timestamps, and `historicalCoverageComplete=false`; this prevents fabricated or duplicate "first" records while preserving exact coverage for future unseen events.

## Mecha research auto-management buffer

Targeted inspection of the same current assembly confirms:

```text
public MechaLab Mecha.lab
public double Mecha.researchPower
public ItemBundle MechaLab.itemPoints
public Dictionary<int, int> ItemBundle.items
public bool GameHistoryData.autoManageLabItems
public void MechaLab.AutoManage()
public void MechaLab.ManageSupply(TechProto techProto)
public void MechaLab.ManageTakeback()
public void MechaLab.GameTick(long time, float deltaTime)
```

With `autoManageLabItems=true`, `ManageSupply` computes each current technology item's remaining point requirement, subtracts points already buffered, rounds the remainder up by 3600, and removes that many real items from the tail of `player.package`; each removed item adds exactly 3600 to `itemPoints`. This reservation occurs before the mecha verifies available research power or produces a hash. `GameTick` consumes buffered points only when `mecha.researchPower`, energy delivery and all required item buffers permit progress. When there is no applicable current technology or automatic management is disabled, `ManageTakeback` returns whole buffered items through the normal player-package path and clears the bundle.

This explains a live package delta that factory-lab observation alone could not: 293 electromagnetic matrices were transferred normally from storage to the player; the package then retained 42 while technology `1703` remained at `242820/288000`. The exact 251-item difference equals `ceil((288000-242820)*20/3600)`, while both factory research labs retained zero blue input and unchanged red input. The matrices were therefore reserved by `MechaLab`, not consumed by a factory lab or lost. `get_player_state` now deep-copies `autoManageResearchItems`, `mechaResearchPower`, and every positive `itemPoints` entry as exact points plus whole-item/remainder views. The live buffer readback itself awaits the next normal Plugin deployment; no API writes or clears this hidden container.

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

## v0.3 logistics-station observation path

Targeted reflection-only metadata inspection of the validated `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85` established the current local-station ownership chain:

```text
public PlanetTransport PlanetFactory.transport
public int EntityData.stationId
public StationComponent[] PlanetTransport.stationPool
public int PlanetTransport.stationCursor
public int StationComponent.id
public int StationComponent.gid
public int StationComponent.entityId
public int StationComponent.planetId
public StationStore[] StationComponent.storage
public SlotData[] StationComponent.slots
```

A station snapshot is accepted only when the positive factory entity still exists, `entity.stationId` is inside the current `stationCursor`, the pool entry's `id` equals that index, and its `entityId` matches the factory entity. Current-assembly decompilation plus the first live PLS exposed a type-specific planet identity rule: `StationComponent.Init(...)` assigns `id/entityId/pcId` and the entity's `stationId`, but does not assign `planetId`; `PlanetTransport.NewStationComponent(...)` calls `GalacticTransport.AddStationComponent(planet.id, station)` only for an interstellar station. A newly built planetary station therefore keeps the native raw sentinel `station.planetId == 0`, while an interstellar station must carry the exact factory planet ID. Spherewright accepts only `0` or the exact local planet for a non-interstellar station, and still requires the exact positive local planet for an interstellar station; another positive planet is rejected in both cases. The public DTO always reports the already bound local factory planet ID. The Plugin then deep-copies primitives and DTOs on Unity's main thread; no `StationComponent`, `StationStore`, `SlotData`, pool or Unity object leaves that thread.

```text
public void StationComponent.Init(
  int id, int entityId, int pcId, PrefabDesc desc,
  EntityData[] entityPool, int extraStorage, bool logisticShipWarpDrive)
public StationComponent PlanetTransport.NewStationComponent(
  int entityId, int pcId, PrefabDesc desc)
public int GalacticTransport.AddStationComponent(int planetId, StationComponent station)
```

The current component exposes these read fields used by the first v0.3 slice:

```text
bool isStellar, isCollector, isVeinCollector
long energy, energyPerTick, energyMax
int pcId -> PowerSystem.consumerPool[pcId].workEnergyPerTick/requiredEnergy
int warperCount, warperMaxCount
int idleDroneCount, workDroneCount, idleShipCount, workShipCount
int PrefabDesc.stationMaxDroneCount, stationMaxShipCount
double tripRangeDrones, tripRangeShips, warpEnableDist
bool includeOrbitCollector, warperNecessary
int deliveryDrones, deliveryShips, pilerCount
bool droneAutoReplenish, shipAutoReplenish
long remoteGroupMask
ERemoteRoutePriority routePriority
int[] needs
```

`StationStore` contains `itemId`, `count`, `inc`, `localOrder`, `remoteOrder`, `max`, `keepMode`, `keepIncRatio`, `localLogic` and `remoteLogic`, plus pure count getters for local/remote supply and demand. The current `ELogisticStorage` values are `None=0`, `Supply=1`, `Demand=2`. Each `SlotData` contains `dir`, `beltId`, `storageIdx` and `counter`; `IODir` is `None=0`, `Output=1`, `Input=2`. A nonzero belt component is additionally resolved through `CargoTraffic.beltPool[beltId]` and accepted only when its component ID matches before copying its entity ID.

Current-assembly decompilation proves the storage UI business path rather than merely the method's existence:

```text
UIStationStorage.OnItemPickerReturn
  -> PlanetTransport.SetStationStorage(station.id, index, itemId,
       stationMaxItemCount + researchedExtra, Supply,
       station.isStellar ? Supply : None, GameMain.mainPlayer)
UIStationStorage.OnMaxSliderValueChange
  -> SetStationStorage(..., round(slider * 100), current local/remote logic, player)
UIStationStorage.OnOptionButton*Click
  -> SetStationStorage(..., current item/max, selected local/remote logic, player)
public void PlanetTransport.SetStationStorage(
  int stationId, int storageIdx, int itemId, int itemCountMax,
  ELogisticStorage localLogic, ELogisticStorage remoteLogic, Player player)
```

`SetStationStorage` clamps the maximum to the model capacity plus the currently researched local/remote storage bonus, forces remote logic to `None` for a planetary station, and refreshes local/galactic traffic when logic changes. Its dangerous branch is equally important: when the requested item differs from a nonempty slot, it calls `Player.TryAddItemToPackage(..., throwTrash:true)`, clears count/inc/orders, and may drop overflow. Spherewright therefore adopts only the safe subset: item ID must be normally unlocked; limits must be positive 100-item UI steps within the current researched capacity; duplicate station items are rejected; the slot must be empty or already assigned to the same item; and both orders must be zero. Clear and replace are not exposed. Prepare binds the separate station configuration hash. Commit snapshots slot count/inc plus every package and in-hand item/count/inc tuple, calls `SetStationStorage` once, and requires exact item/max/logic readback with those inventory fingerprints unchanged; any ambiguous result follows normal write quarantine.

`StationComponent.energyPerTick` is live demand, not the configured maximum. `StationComponent.SetPCState` calls `PowerConsumerComponent.SetRequiredEnergy(...)` from the station's current fill ratio and then assigns `energyPerTick = consumer.requiredEnergy` every tick. The UI instead initializes and writes the maximum through `consumer.workEnergyPerTick`; its display multiplies that per-tick value by 60. The observation DTO therefore reports `requestedChargeEnergyPerTick/requestedChargePowerWatts` as live state and `maximumChargeEnergyPerTick/maximumChargePowerWatts` as configuration. Only the maximum belongs to `configurationStateHash`.

The adopted maximum-charge action mirrors the exact `UIStationWindow` scale instead of accepting an arbitrary energy write. On open, the UI computes slider bounds as integer divisions `(prefabWorkEnergyPerTick / 2) / 50000` and `(prefabWorkEnergyPerTick * 5) / 50000`; the callback assigns `round(50000 * sliderValue)`, while the label displays `round(3000000 * sliderValue)` watts. Spherewright restricts this further to whole 3 MW steps, cross-binds `entity.stationId`, `station.pcId`, `entity.powerConId`, `consumer.id/entityId`, and rejects collectors. Prepare binds the station configuration hash and current value. Commit changes only `consumer.workEnergyPerTick`, then proves the new maximum while consumer identity, current required energy, station requested energy, stored station energy, every storage-field tuple and the complete player package fingerprint remain unchanged in the same main-thread call.

The other UI transforms remain read-only in this slice: drone range stores `cos(degrees / 180 * pi)`; vessel range stores `2400000 * mappedLightYears`; warp distance stores `40000 * mappedAU`; and minimum drone delivery stores `round(slider * 10)` percent with a minimum of one. The public DTO still labels route values raw/settings because no route-setting action has yet adopted their full bounds and readback matrix.

Current-assembly decompilation also proves the exact ordinary fleet-slot paths:

```text
private void UIStationWindow.OnDroneIconClick(int obj)
  accepts only item 5001
  capacity = station building prefabDesc.stationMaxDroneCount (UI fallback 10)
  occupied = idleDroneCount + workDroneCount
  deposit: idleDroneCount += accepted; hand count/inc -= split_inc result
  withdrawal: only idleDroneCount; shift/control calls Player.TryAddItemToPackage

private void UIStationWindow.OnShipIconClick(int obj)
  additionally requires station.isStellar
  accepts only item 5002
  capacity = station building prefabDesc.stationMaxShipCount (UI fallback 10)
  occupied = idleShipCount + workShipCount
  deposit/withdrawal mirrors the drone path

public int StorageComponent.TakeItem(int itemId, int count, out int inc)
public int StorageComponent.AddItemStacked(int itemId, int count, int inc, out int remainInc)
public void Player.NotifyPackageAddItem(int itemId, int count, int inc)
```

The adopted structured action is intentionally narrower than the UI. It requires the player hand to be empty, uses a disposable package copy to prove an exact withdrawal destination before mutation, limits each action to 100 craft, rejects collectors and vein collectors, and rejects player-to-station transfers whenever the package's aggregate proliferator points for that craft are nonzero because the native fleet counter has nowhere to retain them. Prepare and commit bind entity/station/planet/type/position, both capacities, all four idle/working counters and both auto-replenish flags through a dedicated fleet hash. Immediate execution uses the normal package take/add primitives and the exact UI idle-counter field, then verifies player/idle equal-and-opposite deltas and conservation while working craft, the opposite fleet, cargo slots/orders, energy, warpers, station configuration, the player hand and unrelated package grids remain unchanged. Any post-mutation ambiguity enters the existing write quarantine.

Live verification on the first native PLS (`entity 916`, current local planet `104`) closes the adopted local-station subset. A normally built pole (`entity 917`) changed the station from network 0 and `0/180 MJ` to fully served network 1 and natural full charge. `SetStationStorage` then configured empty slot 0 to titanium ingot `1106`, maximum 100, local Demand and remote None without changing inventory. The charge action changed only the configured maximum from 12 MW to 6 MW while full energy and the native 60 kW requested floor remained unchanged. Ten unproliferated drones from the production storage were conserved through player `10 -> 0`, station idle `0 -> 10`; an independent withdrawal/redeposit proved `10 -> 9 -> 10` against player `0 -> 1 -> 0`, with working drones, energy, cargo slots/orders and configuration unchanged. The exact state was normally saved at tick `9522204`. This does not yet validate an interstellar station, vessels, working-craft races, or a real logistics order.

The observation DTO therefore provides three versioned hashes. The live hash covers energy, inventory, orders, fleet activity and needs. The configuration hash excludes those tick-volatile values while binding station identity/type, capacity settings, storage item/limit/logic, route settings and belt topology. The fleet hash binds only the exact station fleet identity/capacity/counters and auto-replenish state, avoiding unrelated cargo or energy churn while still invalidating a launch, return, rebuild, or fleet setting change between inspect and commit.

## Stable research-selection state

`GameHistoryData.CanEnqueueTech(int techId)` decides whether a technology can enter the native queue. The earlier research action bound `CanonicalStateHash.Progression`, which also contains every technology's `hashUploaded`. A powered research lab can advance that value between the read and the next main-thread prepare, so a valid queue request was rejected as stale even though the queue, prerequisites and unlock state were unchanged.

The progression DTO now exposes a second `selectionStateHash`. Its canonical domain binds session/planet, current technology, ordered queue, and each technology's unlock, level, required-hash, lab/queue and prerequisite state. It deliberately omits `hashUploaded`, while the original complete hash remains available for progress observation. Research prepare binds the selection hash, calls `CanEnqueueTech`, and commit re-reads both before invoking the existing native enqueue path. Tests prove that increasing uploaded research changes the complete hash but not the selection hash, while a queue or unlock change invalidates the selection hash.

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

`Player.GameTick` calls `Mecha.GenerateEnergy(deltaTime)` every game tick. The current method first adds `corePowerGen * deltaTime` to `coreEnergy` and clamps to `coreEnergyCap`, independently of the later reactor-fuel branch. Only afterward does it consume `reactorEnergy` or take another item from `reactorStorage`. Consequently an alive stationary mecha has a slow native baseline recovery even with `reactorEnergy=0`, `reactorItemId=0`, and an empty fuel chamber. A 20-second live sample in the current world rose monotonically from about `36.13 MJ` to `37.61 MJ`, approximately `80 kW`, with identical position and player-action state hash. This is an emergency recovery floor, not a practical substitute for fuel or a powered charger during long travel.

The explicit save action binds the active owned session, local planet, current revision, and the high-entropy save name retained internally when Spherewright created that world. Commit calls `GameSave.SaveCurrentGame` only with that exact identity and records `lastOwnedSaveGameTick` after a true return. The path does not enumerate a save directory, discover save names, open a save, or accept a client-provided save name. It is compile/Core/MCP covered and runtime-validated repeatedly in the current owned world, including the M0 acceptance save at tick `2499658` and the later continuation save at tick `2710106`.

## Native same-star flight and reusable pre-flight checkpoint

Targeted current-assembly decompilation additionally confirmed:

```text
public static bool GameSave.SaveCurrentGame(string saveName)
public static void GameSave.ReadHeader(string saveName, bool readImage, out GameSaveHeader header)
public static void DSPGame.StartGame(string loadFile)
public void PlayerMove_Walk.SwitchToFly()
public void PlayerMove_Sail.ResetSailState()
public double PlayerMove_Sail.UseSailEnergy(double acc)
public void PlayerMove_Sail.UseSailEnergy(ref VectorLF3 delta, double costRatio)
```

The flight action resolves only planets in the current star, requires Drive Engine level 2, a living grounded player, no active player order or build preview, a high core-energy ratio, and a conservative core/reactor/fuel energy budget. The current assembly exposes no public `SwitchToSail`. `PlayerMove_Fly.GameTick` enters Sail only after `targetAltitude >= 50`, `currentAltitude > 49`, `horzSpeed > 12.5`, and `thrusterLevel >= 2`; its accepted branch clears a Build command, selects `movementStateInFrame=Sail`, calls `ResetSailState`, synchronizes the sail camera, notifies the game scenario, and publishes the movement-state change. Spherewright first keeps DSP's ordinary vertical/forward input channels asserted until those native conditions are observed, then reproduces exactly those current-version branch side effects. It restores the original input values on Sail, landing, completion, or failure. It never assigns position, planet identity, core energy, or fuel.

Once in Sail, direct destination steering may intersect the origin planet when the target is initially below the local horizon. The controller therefore treats departure as a separate phase: below 500 m it combines radial outward and tangent directions; above that, it continues the tangent escape only while the origin sphere still blocks line of sight. All departure, cruise, and braking velocity deltas go through `PlayerMove_Sail.UseSailEnergy`. The game continues to integrate position, collision, gravity, energy generation/consumption, and landing on ordinary ticks; there is no planet transfer, teleport, fast-travel call, or direct position assignment. The first live `104 -> 102` sample cleared the origin from about 132 m to 499 m, reduced destination-surface distance from about 61 km to 2.1 km, and landed alive. Exceeding the conservative duration estimate does not abandon control while the player is still in space.

Landing completion is intentionally stronger than observing one `Walk` tick. A live return first reported `Walk` while still moving at about 3.41 m/s, then reverted to `Drift` and continued burning fuel. The action now records destination contact separately and requires 600 consecutive ticks with destination planet identity, a living player, `Walk`, and speed at most 0.1 m/s. Any other movement state or excess speed resets the stable interval; 7200 ticks after contact is the bounded failure limit. The corrected return stayed at zero speed in `Walk` for all 600 ticks, and a further 10-second readback showed identical position plus ordinary core recharge.

The first commit toward a destination performs a strictly ordered checkpoint transaction before `SwitchToFly`: generate an internal high-entropy `Spherewright_PreFlight_*` slot; call `SaveCurrentGame`; read the header and require its `gameTick` to equal the just-saved tick; atomically persist a current-user-protected ticket containing checkpoint ID/token, internal slot, embedded primary owned-save identity, source session/revision, origin/destination, game version and state hashes; then bind that ticket to the action. If any step fails, native launch does not start. A retry immediately after loading that checkpoint reuses it instead of creating a newer save.

Checkpoint reload has its own two-phase surface and never accepts a save name. Prepare requires the protected token and revalidates the exact internal file/header plus a recovery-required/interrupted-flight lifecycle; commit preflights idempotency capacity, arms exact-session adoption, and calls `DSPGame.StartGame` with the ticket's internal slot. Adoption ignores the animated menu-demo `GameData`, waits until `GameLoader` is gone and `localPlanet` is populated, then verifies the embedded primary owned-save identity, saved-tick window, origin planet, peaceful/non-sandbox/1x settings, and checkpoint ID. Current-version `GameData.Import(BinaryReader)` clears `DSPGame.LoadFile` before it reads the saved game tick, so that transient static field is not a valid post-load proof; exact file/header revalidation immediately before the sole internal `StartGame` call plus the final embedded identity and bounded tick window provide the proof instead. Repeated failed attempts may load the same pre-flight state; stable success removes the capability, a covering primary save retires the ticket, and the ticket expires after 24 hours. Live checkpoints have been adopted repeatedly before successful physical flights, including the latest `102 -> 104` return, which failed twice from the same checkpoint before the third attempt landed and the primary world was saved at tick `7198197`.

## Exact player-order ownership and termination

Targeted Mono.Cecil inspection of the current assembly confirmed:

```text
public void Player.Order(OrderNode order, bool enqueue)
public void Player.AbortOrder()
public OrderNode Player.currentOrder { get; }
public void PlayerOrder.Order(OrderNode order)
public void PlayerOrder.Abort()
```

For `enqueue=false`, `Player.Order` calls `PlayerOrder.Order`; that method passes the supplied `OrderNode` through `ReachTest` and assigns the same reference directly to `currentOrder`. `Player.AbortOrder` delegates to `PlayerOrder.Abort`, which dequeues the next order into `currentOrder`. This gives Spherewright an exact ownership identity that is stronger than comparing an order's target coordinates after DSP has normalized or snapped them.

The action coordinator now creates the move or mine `OrderNode`, stores that exact reference on its action record, and submits it to `Player.Order`. Completion, stall, power-starvation, and global-timeout paths call `AbortOrder` only when `ReferenceEquals(player.currentOrder, action.PlayerOrder)` is true. A later manual or external order is therefore not stopped. The former move check compared the live target with the requested surface point within `0.1 m`; a live move was declared complete at `1.5 m` but its game order survived, repeatedly spent each small recharge pulse, and drained roughly `101 MJ` before a mine order replaced it. The exact-reference rule removes that split-brain terminal state.

## Resource-node identity and bounded manual harvest

Factory `objectId` values and resource `nodeId` values are different pool namespaces even when their integers happen to match. A miner snapshot exposes its covered resource identities through `resourceNodeIds`; those values, or IDs returned by `list_resource_nodes`, are the only valid inputs to `inspect_resource_node` and `prepare_harvest`. For example, factory entity `106` is a coal miner near the red-matrix line and covers resource nodes `308, 309, 312, 313, 318, 321, 325`, while resource node `106` is an unrelated iron vein on the other side of the planet.

`PrepareHarvestOnMainThread` now requires the freshly inspected resource to report `WithinPlayerBuildArea=true`. A resource outside the player's current normal interaction area returns retryable `TARGET_OUT_OF_RANGE` and instructs the caller to use bounded surface waypoints before re-inspection. The former implementation exposed `EstimatedDistance` but did not enforce a maximum; an accidental factory-ID-as-node-ID request therefore started a remote Mine order and spent the 7200-tick global window walking toward an unrelated vein. Range rejection makes harvest an interaction primitive again rather than an unbounded navigation shortcut.

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

`OnItemPickerReturn` assigns `item.ID` to `InserterComponent.filter`, then assigns the same ID to the sorter's entity sign and sets `iconType=1`; reset writes zero to all three values. Neither handler tests `stage`, `time`, or carried cargo. Because the current version exposes no safer business setter, the Plugin adapter reproduces only those exact assignments but narrows the callable subset: it requires the precise built sorter/component identity, both connection targets, and zero carried item/count/stack/inc at both prepare and commit. A dedicated root factory `configurationStateHash` binds identity, rotation, endpoints, reverse connection table, current filter, stack count, and held-cargo buffers while excluding native `stage/time` return progress. This permits an already connected sorter to be configured during a cargo-free `Returning` phase without making every intervening tick stale; any newly picked cargo changes the hash and independently fails the commit guard. The full factory hash continues to expose stage and progress for observation. Candidate session `73b4019b-c5cc-4f90-b1f4-bc4abc6d49c6` first runtime-verified the original empty-`Picking` path on sorter `211` with refined-oil filter `1114`; the strictly recovered current world independently validated refined-oil sorter `164` and hydrogen sorter `181`, including carried cargo and distinct destinations. The cargo-free return-window extension has current-assembly handler evidence and automated hash/policy tests, but remains pending live validation after the next normal save/restart deployment.

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

Targeted Mono.Cecil inspection for the post-M0 water chain confirms `EMinerType.Water = 1`, `PrefabDesc.minerType`, `PrefabDesc.waterPoints`, `PrefabDesc.waterTypes`, `PlanetData.waterItemId`, and the native water checks inside `BuildTool_Click.CheckBuildConditions`. Item `2306` is therefore exposed in the runtime building catalog as role `water-pump` when unlocked; it remains on the ordinary click-build validation and drone-construction path rather than accepting a vein ID or synthesizing water. Live verification has now closed the earlier gap: ordinary replicator recipe `49` produced one owned pump, native click-build accepted entity `752` at the water candidate, drones completed it, and the full-powered pump emitted runtime water item `1000`. The pump has a belt output but no ordinary inserter attachment pose, so the validated extraction topology is `752 -> 758…754 -> sorter 759 -> storage 753`; storage grew `9 -> 31` and the per-save journal recorded first production-line water at tick `6245078`.

The raw named-pipe DTO and the MCP wrapper intentionally expose different coordinate shapes. A direct `Invoke-SpherewrightBridgeRequest -Method 'prepare_build'` payload must serialize `PrepareBuildRequest.PreferredPosition` as `preferredPosition = @{ x = ...; y = ...; z = ... }`. The MCP tool instead exposes the convenience scalars `preferredPositionX/Y/Z` and combines them into that DTO. Passing those MCP-only scalar names to the raw bridge is not equivalent: the current JSON reader ignores the unknown properties and falls back to the near-player candidate search. Therefore precision construction uses an explicit prepare/read/commit sequence and bounds-checks `plannedPosition`, `plannedYaw`, `buildKind`, and endpoint identities before any commit. This rule caught a red-matrix storage request whose intended coordinates were near lab `256` but whose prepared fallback was near the player; the resulting ordinary storage `259` is retained as a separate utility object, while the corrected vector request produced storage `260` at the validated lab-side pose and sorter `261` connected `256 -> 260`.

A successful core-building prepare proves only that building's click-build legality. It does not prove that a future inserter can span from that building to another endpoint. The power-engine layout provided a direct contrast: storage `286` was an ordinary accepted build, but the later `285 -> 286` inserter prepare returned `BUILD_CONNECTION_INVALID/TooFar`; no inserter was committed. A tangent-plane ring search found the closer accepted storage `287`, after which separate prepares accepted `285 -> 288 -> 287` and `26 -> 289 -> 285`. Multi-device layout therefore reserves connection margin, checks the snapped position rather than the requested float vector, and treats every sorter connection as its own prepare-time proof. A valid but unreachable auxiliary building is retained when no safe dismantle action exists.

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

No unrelated pre-existing save was enumerated, loaded, or read during this research or runtime verification. The ordinary non-sandbox world, structured observation, normal-play actions and complete first-red-matrix path have live runtime verification. Same-star flight and its exact reusable checkpoint reload currently have assembly, compile and automated MCP evidence only; live validation waits for a safe same-save Plugin deployment.
