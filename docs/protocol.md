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
spherewright_get_gameplay_journal
spherewright_get_local_star_system
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
spherewright_prepare_interplanetary_flight
spherewright_commit_interplanetary_flight
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
spherewright_prepare_quarantine_reconciliation
spherewright_commit_quarantine_reconciliation
spherewright_prepare_resume_owned_game
spherewright_commit_resume_owned_game
spherewright_prepare_reload_flight_checkpoint
spherewright_commit_reload_flight_checkpoint
```

`prepare_new_game` and `commit_new_game` now describe only a peaceful 1x non-sandbox world. The old sandbox basic-production-line methods are not registered as MCP tools and are excluded from M0.

`spherewright_prepare_configure_building` accepts `production`, `research`, `sorter-filter`, `logistics-station-storage`, and `logistics-station-charge` modes. Sorter-filter mode additionally carries `filterItemId`, requires a connected sorter with zero carried item/count/stack/inc, and binds its dedicated root `configurationStateHash`: identity, topology, current filter, and held-cargo buffers are included while ordinary return progress and stage are excluded. Prepare and commit both reread the cargo-free invariant, so a sorter may be filtered during its native empty `Returning` phase but never while carrying cargo. Logistics-station-storage mode separately binds the nested station `configurationStateHash`, one slot index, one unlocked item, a 100-item-step maximum, and local/remote `none|supply|demand` logic. It accepts only an empty slot or the same existing item with no outstanding orders, forbids clear/replace, and commits once through `PlanetTransport.SetStationStorage`; immediate readback must prove item/limit/logic while slot count/inc and every package/in-hand item/count/inc tuple remain unchanged. Logistics-station-charge mode binds the same station configuration hash plus `stationMaximumChargePowerWatts`; it accepts only 3 MW UI steps inside the exact prefab's integer-slider range of one-half through five times default work energy. Commit writes only the same `PowerConsumerComponent.workEnergyPerTick` field as `UIStationWindow`, then proves maximum-power readback while requested power, consumer identity, station inventory and player inventory remain unchanged. The refuel pair binds an exact current player/fuel-chamber snapshot and commits through `Mecha.AutoReplenishFuel`; readback must prove equal-and-opposite count changes and conserved proliferator points. The normal save pair binds the current owned-session revision and commits only through `GameSave.SaveCurrentGame` with the exact internally retained primary save name. An interplanetary-flight commit first saves a separately named internal checkpoint, verifies its header tick, and persists its reusable provenance ticket before native flight begins. None of these operations can address an external save or inject an item/energy value.

`spherewright_get_gameplay_journal` is a read-only, owned-session-gated view of a current-user-protected file keyed by a one-way hash of the internal owned-save identity. It records first manual and first production-line output independently per item, plus first technology and upgrade selections, with ISO-8601 wall-clock time, raw game tick, and formatted in-save time. `durableThroughSequence` identifies the highest sequence confirmed on disk; `persistencePending` and `persistenceError` distinguish newer memory-only entries from durable evidence. It never returns the raw save identity or an absolute journal path. A journal created for an already progressed save explicitly reports incomplete historical coverage instead of inventing earlier timestamps.

`spherewright_get_progression_state` returns both a complete live `stateHash` and a dedicated `selectionStateHash`. The complete hash includes each technology's naturally increasing uploaded research hash and remains suitable for exact observation comparisons. Research prepare/commit instead require `expectedSelectionStateHash`: it binds the current technology, ordered queue, unlock and level state, required hashes, lab/queue classification, and prerequisite IDs, but excludes the tick-live uploaded amount. Natural lab progress therefore cannot make a queue-only decision stale between inspect and prepare, while a completed prerequisite, queue edit, current-tech transition, or unlock still invalidates the plan. `CanEnqueueTech` is checked again at both prepare and commit.

`spherewright_list_factory_entities` with `componentKind=station`, and `spherewright_inspect_factory_entity` for the resulting positive entity ID, include a deep-copied `logisticsStation` object for a completed station. The object cross-binds entity, local station, galactic station and planet identities; reports stored energy, current requested charging energy/power, configured maximum charging energy/power, fleet counts, raw route settings, every configured or empty storage slot, and every station belt port; and includes separate `stateHash` and `configurationStateHash` values. The full hash changes with live requested charge, energy, inventory, orders and fleet activity. The configuration hash excludes those volatile values but binds the consumer's configured maximum charge, identity, station type, slot item/limit/logic, route settings and belt topology. Trip/delivery/warp values remain explicitly named `*Raw` or `*Setting`; their UI transforms are documented locally, but no route-setting writer is adopted yet. The only station writes in this slice are the restricted storage-slot and maximum-charge configurations above; no action directly fills, clears, replaces, or injects station inventory.

Quarantine reconciliation is not a force-unlock: it is available only for the exact retained outcome-unknown build action and clears quarantine only after the item decrement, all new entity/component identities, and directed topology form one unchanged proof. It never repeats the build. Owned-game resume is likewise not a save picker: it accepts only a protected one-time token. A healthy planned restart loads only the exact primary owned save already sealed in the ticket; quarantine recovery alone may load DSP's fresh fixed `LastExit` to preserve unsaved progress. Before commit, the selected file header must prove the ticket's minimum game tick. Adoption still requires high-entropy owned identity, minimum tick, planet, peaceful/non-sandbox/1x settings, and source-process shutdown proof. Consumption first persists a token-hash tombstone, so a failed best-effort file deletion cannot resurrect the old token.

Flight-checkpoint reload is a narrower repeatable recovery path. It accepts only the protected token created immediately before that flight, never accepts or enumerates save names, revalidates the internally generated name and exact saved tick, and loads through `DSPGame.StartGame`. Adoption additionally requires the embedded primary owned-save identity, origin planet, peaceful/non-sandbox/1x settings, and exact checkpoint ID. Reload is allowed only for a persisted `recovery_required` flight, or for an interrupted in-flight ticket whose source DSP process terminated. A failed retry returns to the same checkpoint. Stable flight success immediately removes the reload capability; a primary save covering the success tick retires the ticket, and a newer exact primary header supersedes legacy tickets at startup. Tickets expire after 24 hours.

The public surface contains no composite red-matrix or legacy sandbox operation. Missing future methods return no simulated data and must not be substituted with historical sandbox code.

## Request rules

Every save/player/factory request carries the active `sessionId`; planet-bound requests also carry `planetId`. A mutable prepare binds the current complete target/resource state and returns an opaque short-lived plan. Commit carries the plan token and a UUID idempotency key. Factory observations expose both `stateHash` for complete mutable device state and `endpointStateHash` for build connections. Build source/destination requests use `endpointStateHash`, which binds identity, pose, and existing connections without becoming stale solely because a miner, belt, or assembler advances normal production.

Ordinary sorter construction records any pre-existing same-item entities at the prepared source pose before calling DSP's native prebuild path. Completion excludes those IDs before topology verification, so two legal sorters that share the same building-side position cannot cause the newer action to be attributed to the older sorter.

New-game creation and exact owned-game resume are the ordinary main-menu mutations and have no active session/planet envelope yet. Flight-checkpoint reload may replace only the exact matching active owned game or run from an idle main menu; it can interrupt no active normal action except its own bound flight. New-game creation generates the save name and fixes peaceful mode, sandbox disabled, and resource multiplier 1x. Neither recovery path can enumerate or accept a save name. The primary-save resume fallback is internal-only, requires that exact protected ticket identity and a fresh file timestamp, and intentionally restores no post-ticket unsaved action.

A stale session returns `STALE_SESSION`, an unowned session returns `SESSION_NOT_OWNED`, a stale target/resource snapshot returns `STALE_STATE`, and any confirmed or unknown sandbox state blocks gameplay commits.

Responses never contain the Pipe name, bridge authentication token, descriptor path, stack trace, absolute game path, raw save identity/content, or raw game component. Opaque plan tokens plus one-time resume/reusable checkpoint tokens appear only in their intended authenticated structured fields and must not be copied into logs or evidence documents. List methods are bounded and use session/planet/snapshot/filter-bound cursors.

## Action semantics

All normal-game mutations use separate `prepare_*` and `commit_*` methods. Prepare is read-only even when writes are disabled. Commit is single-flight by `(sessionId, idempotencyKey)`, rereads all preconditions on the Unity main thread, and records a queryable action state.

If a commit may have been accepted but its result is unavailable, the MCP mapping returns `ACTION_OUTCOME_UNKNOWN`; clients must retry with the same request/key or query `get_action_result`. They must never use a new key to guess-replay the action.
