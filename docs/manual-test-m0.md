# M0 manual integration test — First Red Matrix

This procedure applies only to a fresh world created by the current Spherewright Plugin process. Do not open the Load Game screen, enumerate save names, or load any existing save. Do not use Computer Use, visual recognition, keyboard/mouse macros, or manual in-game assistance after DSP reaches the main menu.

Sections whose tools are not implemented remain blocked; do not substitute the legacy sandbox basic-line tools.

## 1. Safe startup

1. Exit DSP normally.
2. Run `./scripts/sync-game-refs.ps1` and `./scripts/install-dev-plugin.ps1`.
3. Confirm the generated configuration contains `Safety.AllowWrites=false`.
4. Start DSP normally and stop at the main menu.
5. Verify one Plugin load, the game/protocol versions, protected Pipe startup, wrong-token rejection, authenticated status, and protocol-only MCP stdout.
6. Call each available prepare tool with writes disabled and verify a `WRITES_DISABLED` blocker with no game mutation.

## 2. Create an ordinary owned world

1. Exit DSP, set only local `Safety.AllowWrites=true`, restart, and remain at the main menu.
2. Call `spherewright_prepare_new_game` with a seed and supported star count.
   During DSP startup, retry only `BRIDGE_NOT_READY` or `REQUEST_TIMEOUT`; do not commit until prepare succeeds after prototype preload.
3. Confirm it reports peaceful mode, 1x resources, `sandbox=false`, an unexpired plan, and no started game.
4. Commit once with a UUID idempotency key and wait through structured status only.
5. Read session/player/progression state and prove owned session, `confirmedPeaceful`, sandbox confirmed disabled, 1x resources, normal initial inventory/technology, and no artificial item delta.
6. Retry the same commit and prove no second new-game action.

## 3. Verify each normal-play primitive

For move, harvest, handcraft, build, transfer, configure, research, refuel, and explicit save:

1. Inspect the exact target and current player/resource state.
2. Prepare with writes disabled once, then with writes enabled; verify prepare has no side effects.
3. Commit with a fresh UUID, poll `get_action_result`, and inspect before/after.
4. Prove real distance, inventory/energy, ingredients, construction time, connections, unlocks, and game ticks as applicable.
5. Retry the same commit and prove idempotent replay.
6. Exercise stale target/resource, insufficient inventory, out-of-range, locked recipe/tech, invalid build, and response-loss paths without contaminating a user save.
7. For refuel, prove the package decrement, fuel-chamber increment, and total count/proliferator-point conservation; never write mecha energy directly.
8. For save, prove `lastOwnedSaveGameTick` is recorded for the exact current owned save name; never enumerate or open another save.
9. Complete at least one technology and prove the result modal is dismissed through the native `FadeOut()` lifecycle without Computer Use or synthesized input.
10. Configure two idle empty refinery-output sorters with distinct hydrogen/refined-oil filters; prove component filters, entity signs, topology, and carried-cargo state before allowing production.

## 4. First-red-matrix run

From another fresh ordinary owned world, use only MCP tools to:

1. Walk, hand-harvest, and handcraft the bootstrap items.
2. Establish normal power, mining, smelting, component manufacturing, belts, and sorters.
3. Produce blue matrices and research the runtime-discovered red-matrix prerequisite normally.
4. Mine/process coal into energetic graphite and extract/refine crude oil into hydrogen.
5. Use filtered refinery outputs so hydrogen and refined oil cannot ambiguously share a sorter, then configure and supply a matrix lab for red matrices.
6. Observe red-matrix count change from 0 to at least 1 through production ticks.
7. Re-read the complete upstream chain, power, logistics, recipe, buffers, technology, player inventory, and save result.

The audit must contain no item grant, storage/buffer insertion, direct technology write, direct `BuildFinally`, teleport, sandbox activation, save edit, or game-speed change.

## 5. Shutdown

1. Save the owned world through `spherewright_prepare_save` and `spherewright_commit_save`, then verify `lastOwnedSaveGameTick` through session state.
2. Exit DSP normally.
3. Confirm no unhandled Spherewright exception and that the runtime descriptor is removed.
4. Restore `Safety.AllowWrites=false`.
