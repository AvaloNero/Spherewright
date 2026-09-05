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
- Local live: same-batch `b9e74bd` Plugin deployment matched all four DLL hashes after normal exit. Protected resume restored the same long-running owned world from minimum tick `19369335`, automatically saved `19369366`, and retained Journal `51/51` durable. A fresh source Release MCP Host advertised 54 tools/1 resource; its real `spherewright_get_foundry_plan` call at tick `19392231` succeeded. Target motor `1203 @ 30/min` returned depth 3, 8 machines, base working power `2520000 W`, external iron ore `120/min`, copper ore `15/min`, and no byproducts. Two equivalent calls had the same material hash. Rate zero and a wrong session were rejected as `INVALID_REQUEST` / `STALE_SESSION` without any gameplay action.
- The live build catalog independently reported smelter `2302`: speed `10000`, work energy `6000/tick`; assembler `2303`: speed `7500`, work energy `4500/tick`. Both were unlocked/available grade 1. The plan therefore cost four of each, excluding logistics and power infrastructure. No machine was constructed in this check.
- Not yet claimed: native site feasibility, sufficient ongoing automatic supply, exact belt/sorter/storage/power cost, durable action graph, restart reconciliation, or completed Foundry construction. The current public draft explicitly returns `executable=false` and lists these remaining checks. Normal owned resume is proven here, **not** resumable Foundry construction.
- Cross-computer live validation of this new capability has not been performed.

Local live stage readback (rates are items/game minute, not newly measured factory output):

| Item | Recipe | Required rate | Base rate per machine | Machines | Installed capacity |
|---|---|---:|---:|---:|---:|
| Iron ingot 1101 | 1 | 90 | 60 | 2 | 120 |
| Gear 1201 | 5 | 30 | 45 | 1 | 45 |
| Magnet 1102 | 2 | 30 | 40 | 1 | 40 |
| Copper ingot 1104 | 3 | 15 | 60 | 1 | 60 |
| Magnetic coil 1202 | 6 | 30 | 90 | 1 | 90 |
| Electric motor 1203 | 97 | 30 | 22.5 | 2 | 45 |

Rejected alternatives: UI request-dependent `refProductSpeed` cache; hard-coded game recipes; assumed 1× machine speed; finite inventory as sustained supply; passing mutable runtime pools into Core; treating a material calculation as permission to build.
