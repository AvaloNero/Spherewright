# Bridge protocol

Protocol version: `1`.

Each message is a four-byte little-endian signed length followed by UTF-8 JSON. Negative lengths and lengths over 1 MiB are rejected before allocation. A zero-length frame is accepted by the frame layer and rejected by the JSON layer.

The first message on every Pipe connection must be an authenticated `handshake` carrying the active `bridgeInstanceId` and startup token from the current-user-only runtime descriptor. Any pre-authentication request or invalid credential closes the connection.

## Current public MCP surface

```text
spherewright_get_status
spherewright_get_session_state
spherewright_get_player_state
spherewright_get_progression_state
spherewright_get_recipe_catalog
spherewright_get_build_catalog
spherewright_get_power_summary
spherewright_get_action_result
spherewright_list_resource_nodes
spherewright_inspect_resource_node
spherewright_list_factory_entities
spherewright_inspect_factory_entity
spherewright_list_assemblers
spherewright_inspect_assembler
spherewright_prepare_new_game
spherewright_commit_new_game
spherewright_prepare_move
spherewright_commit_move
spherewright_prepare_harvest
spherewright_commit_harvest
spherewright_prepare_handcraft
spherewright_commit_handcraft
spherewright_prepare_select_research
spherewright_commit_select_research
spherewright_prepare_build
spherewright_commit_build
spherewright_prepare_configure_building
spherewright_commit_configure_building
spherewright_prepare_transfer
spherewright_commit_transfer
spherewright_prepare_refuel
spherewright_commit_refuel
spherewright_prepare_save
spherewright_commit_save
```

`prepare_new_game` and `commit_new_game` now describe only a peaceful 1x non-sandbox world. The old sandbox basic-production-line methods are not registered as MCP tools and are excluded from M0.

`spherewright_prepare_configure_building` accepts `production`, `research`, and `sorter-filter` modes. Sorter-filter mode additionally carries `filterItemId`, requires a connected idle empty sorter, and binds its topology, stage, carried cargo, and current filter. The refuel pair binds an exact current player/fuel-chamber snapshot and commits through `Mecha.AutoReplenishFuel`; readback must prove equal-and-opposite count changes and conserved proliferator points. The save pair binds the current owned-session revision and commits only through `GameSave.SaveCurrentGame` with the exact internally retained owned save name. None of these operations can address an external save or inject an item/energy value.

The public surface contains no composite red-matrix or legacy sandbox operation. Missing future methods return no simulated data and must not be substituted with historical sandbox code.

## Request rules

Every save/player/factory request carries the active `sessionId`; planet-bound requests also carry `planetId`. A mutable prepare binds the current complete target/resource state and returns an opaque short-lived plan. Commit carries the plan token and a UUID idempotency key.

New-game creation is the only main-menu mutation and has no session/planet yet. The server generates the save name and fixes peaceful mode, sandbox disabled, and resource multiplier 1x.

A stale session returns `STALE_SESSION`, an unowned session returns `SESSION_NOT_OWNED`, a stale target/resource snapshot returns `STALE_STATE`, and any confirmed or unknown sandbox state blocks gameplay commits.

Responses never contain the Pipe name, authentication token, descriptor path, stack trace, absolute game path, save contents, or raw game component. List methods are bounded and use session/planet/snapshot/filter-bound cursors.

## Action semantics

All normal-game mutations use separate `prepare_*` and `commit_*` methods. Prepare is read-only even when writes are disabled. Commit is single-flight by `(sessionId, idempotencyKey)`, rereads all preconditions on the Unity main thread, and records a queryable action state.

If a commit may have been accepted but its result is unavailable, the MCP mapping returns `ACTION_OUTCOME_UNKNOWN`; clients must retry with the same request/key or query `get_action_result`. They must never use a new key to guess-replay the action.
