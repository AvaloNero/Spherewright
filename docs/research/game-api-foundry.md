# v0.4 Foundry DSP API evidence

## Baseline and boundary

- DSP `0.10.34.28529`, BepInEx `5.4.17.0`; inspected 2026-09-05.
- `Assembly-CSharp.dll` SHA-256: `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`.
- Read-only ILSpy inspection of the local assembly; no decompiled code or game binaries are redistributed.
- Plugin reads occur only on the Unity main thread after the existing exact-owned-session/local-planet check. Core receives copied DTOs, not game objects.

## Machine capacity and power

Current public fields:

```text
PrefabDesc.assemblerSpeed       int
PrefabDesc.labAssembleSpeed     int
PrefabDesc.workEnergyPerTick    long
RecipeProto.TimeSpend          int
RecipeProto.Type               ERecipeType
RecipeProto.Items/ItemCounts/Results/ResultCounts   int[]
```

`PrefabDesc` converts assembler `speedf` and lab `assembleSpeed` to integer units by multiplying by `10000`. `PlanetFactory` passes these exact values to `FactorySystem.NewAssemblerComponent(entityId, desc.assemblerSpeed)` and `NewLabComponent(entityId, desc.labAssembleSpeed)`. Recipe execute data uses `TimeSpend * 10000` for its ordinary cycle duration. At 60 ticks/second the unproliferated full-power capacity is therefore:

```text
items/game minute = 3600 * prefabSpeedRaw * outputCount / (TimeSpend * 10000)
working watts     = workEnergyPerTick * 60
```

The build catalog now exposes nullable `productionSpeedRaw` and `workEnergyPerTick`. Unknown or nonpositive speed is not replaced with an invented 1× value. Assembler/smelter/refinery roles must match the runtime recipe type; matrix labs accept `Research` recipes for matrix production, not arbitrary research unlocks. Other production mechanisms, including fractionators and mining, are not simulated by this compiler.

This is a **base-capacity budget**, not measured throughput. It does not credit proliferator bonuses, sorter stack upgrades, spare electricity, input buffers, or existing miners. Actual component rates and transport must still be verified with [Overseer](./game-api-overseer.md).

## Material compilation

`FoundryPlanCompiler` reuses `RuntimeDependencyGraphBuilder`, then selects unlocked runtime producers with compatible available buildings. Explicit item/recipe/building choices take precedence; otherwise selection is deterministic (single-output recipe, recipe ID, building grade, building ID). Raw `ItemProto.isRaw` entries stop recursion unless explicitly recipe-selected; other externally supplied intermediates must be named explicitly. A cycle or unusable selection is rejected, not silently fed from inventory.

Demand is aggregated in reverse topological order before sizing shared upstream producers. Output batch sizes are used for both cycles and rates. Coproducts are listed as required sinks and are not silently credited as free inputs. Bounds are 64 stages, 16 recipe levels and 256 production machines, with finite catalog/choice sizes and checked decimal/integer budgets. Unrepresentably small flows fail closed.

The material hash includes selected recipe types, times, input/output counts, machine speed/power, demand and explicit supply boundaries. It excludes session and capture tick so an identical calculation is reproducible after restart; **it is not a state hash, ownership proof or write token**. In particular, doubling all recipe batch counts can preserve material demand and machine count but changes capacity and must change this hash.

## Validation status and remaining work

- Offline: shared-input aggregation, 0.75× production, output batches, exact choices, cycles, locked inputs, unknown speeds, bounds/overflow/underflow, canonical hashes and read-only MCP mapping are covered by automated tests.
- Full current-DSP Release compilation is checked separately from the no-game-DLL test suite.
- Local live: the pre-existing runtime catalog confirms the long-running owned world's iron/copper → gear/coil → motor recipes `1/2/3/5/6/97` are unlocked. This observation does not validate the new compiler until its newly built Plugin is installed and queried.
- Not yet claimed: native site feasibility, sufficient renewable supply, exact belt/sorter/storage/power cost, durable action graph, restart reconciliation, or completed Foundry construction. The current public draft explicitly returns `executable=false` and lists these remaining checks.
- Cross-computer live validation of this new capability has not been performed.

Rejected alternatives: UI request-dependent `refProductSpeed` cache; hard-coded game recipes; assumed 1× machine speed; finite inventory as sustained supply; passing mutable runtime pools into Core; treating a material calculation as permission to build.
