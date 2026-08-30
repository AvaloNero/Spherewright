# First-red-matrix capability audit

This audit maps the end-to-end acceptance chain to the current structured surface. It is not an autonomous planner and contains no hard-coded DSP prototype plan. The external Agent must select current item, recipe, technology, and building IDs from live catalogs in the one owned validation session.

| Required capability | Structured path | Safety/readback | Current evidence |
|---|---|---|---|
| Create the test world | `prepare_new_game` / `commit_new_game` | Peaceful, 1x, sandbox disabled; exact next-`GameData` ownership; generated save identity | Runtime verified on the current DSP build |
| Discover the red chain | `get_recipe_catalog.firstRedMatrixDependencies` | Graph is built from current runtime recipes; alternatives and co-products retained | Core co-product traversal test passed; refreshed runtime graph pending |
| Bootstrap movement and resources | player/resource reads plus move/harvest actions | Real surface distance, energy, node reduction, and observed inventory yield | Runtime verified for ordinary early-game resources |
| Handcraft prerequisites | recipe catalog plus handcraft action | Current unlock, exact ingredients, native forge task, elapsed ticks, output readback | Runtime verified for early bootstrap items |
| Maintain mecha energy | player fuel snapshot plus refuel action | Native fuel type/grid/stack path; exact count and proliferator-point conservation; no energy write | Build/Core/MCP verified; runtime pending |
| Select and complete technology | progression read plus select-research action; lab research configuration | Current prerequisites/queue, active technology, matrix buffers, normal research ticks | Early technologies runtime verified; modal auto-ack and matrix-tech run pending |
| Build power and production devices | build catalog plus move/build actions | Unlocked owned items, native build validation, ordinary prebuilds and drones, topology/component readback | Wind, pole, miner, lab, smelter, belt, and sorter runtime verified |
| Mine coal and smelt energetic graphite | resource/build/configure/logistics/power reads and actions | Real miner output, unlocked smelting recipe, powered buffers and connections | Generic path verified on iron; coal/graphite run pending |
| Extract crude oil | oil vein read plus resource-building action | Exact oil node/state binding, current oil-extractor validation, drone completion, miner-node readback | Compile-verified against current DLL; runtime pending |
| Refine hydrogen | refinery build/configuration plus belts/sorters | Refine recipe compatibility, ordinary power/ticks, all input/output buffers exposed | Compile-verified against current DLL; runtime pending |
| Separate refinery co-products | `configure_building` with `mode=sorter-filter` | Connected idle empty sorter; topology/stage/cargo/filter bound; exact UI component/sign assignment and readback | Build/Core/MCP verified; runtime pending |
| Produce a red matrix | matrix-lab production configuration plus filtered inputs | Current red recipe, normal buffers/power/ticks; lab output exposes target item/count | Runtime pending |
| Prove success | session, progression, recipe, factory, power, player, and action reads | One continuous owned session; target red item output changes from 0 to at least 1; no unexplained item delta | Runtime pending |
| Persist final evidence | prepare/commit save | Exact current owned save name and revision only; normal DSP save API; confirmed game tick | Build/Core/MCP verified; runtime pending |

## Required next runtime boundary

After confirmation, the next Plugin process creates one ordinary peaceful 1x non-sandbox world. The newly added research-result acknowledgement, refuel, sorter-filter, explicit-save, and corrected dependency graph paths are checked as early steps of that same candidate acceptance session; if they pass, the run continues without changing worlds through the first red matrix and final explicit save. If a failure invalidates or quarantines the session, Spherewright stops before creating any replacement world and reports the need for another confirmation.

Per the user's current instruction, installation, DSP restart, and creation of that next validation world remain paused until explicit confirmation.
