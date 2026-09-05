# Spherewright Agent playbook: opening and core operation

Read this playbook before the first gameplay action in a session, especially after creating a new world. It is also available from the MCP resource `spherewright://agent/playbooks/opening-movement-v1`.

## Session entry and ownership

1. Call `spherewright_get_status`, read this resource, then fresh-read the session and player before deciding what to do. Keep using the current session, planet, revision, and state hashes; never reuse a snapshot from before a load, restart, or player intervention.
2. At the idle main menu, honor the user's intent and use only an advertised protected resume path or the normal new-world flow. Never guess, enumerate, or pass a save name.
3. A world manually loaded by the player is restricted. The only adoption path is save-import prepare, showing its exact confirmation prompt, waiting for an explicit reply in a later user message, and then saving to an independent owned copy. Do not claim Journal history from before that boundary.
4. If writes are disabled, the world is unowned, or write health is not healthy, remain read-only until the documented ownership or recovery flow succeeds.

## Commit discipline

1. Fresh-read the session and player before prepare. Use the current session, planet, revision, and player state hash.
2. Prepare only the next intended action and commit it promptly. Plans are short-lived; do not stockpile tokens or flood the server with candidate prepares. On a stale-state rejection, fresh-read and prepare again.
3. Give each logical commit one UUID idempotency key. If the caller loses a response or times out, query the returned action when known or retry the exact same request with the same key. Never generate a new key to replay an uncertain mutation.
4. After every `spherewright_commit_*`, poll every returned `actionId` with `spherewright_get_action_result` until `terminal=true`. A commit acceptance or host timeout is not completion. If a synchronous commit returns no `actionId`, use its documented terminal result and then fresh-read.
5. If an outcome remains unknown or write health enters quarantine, stop all new writes. Reconcile only the retained exact action when the API offers a proof, otherwise use the protected owned-world restart path. Never repeat the original mutation speculatively.
6. After a successful Move, fresh-read the player. Continue only after `movementState=Walk`, speed is at most about `0.1 m/s`, and core/fuel energy is adequate for the next leg.

## Energy and approach

- Before a long move, harvest, construction batch, or flight, check core energy, the fuel chamber, usable inventory fuel, and the intended charging route. Refuel only with the native refuel prepare/commit pair, or move once to a known safe point inside a powered charging network and wait for fresh energy growth. Do not keep probing a blocked path until the core is empty.
- `prepare_move` validates a finite target, not path clearance. `prepare_harvest` also does not prove that its automatic approach corridor is clear. Near the landing capsule or factory buildings, first use a short tangent waypoint to put the obstacle behind the player before committing the harvest, even when harvest prepare succeeds from the other side.
- Once a movement or harvest action has been accepted, poll that action rather than starting a competing player order. A local wait timeout never authorizes a duplicate commit.

## Stalled Move recovery

- If the terminal result says `failureKind=position_stalled` or `failureKind=route_stalled`, **do not submit the same target again**. Fresh-read before every new candidate.
- If one nearby obstacle is identifiable, project the direction from the obstacle toward the player onto the player's local spherical tangent plane. Prepare/commit one target about **5 m** along that away direction, reprojected to the planet surface.
- If several obstacles are nearby, or the landing capsule/base cannot be identified reliably, form two orthogonal axes on the local tangent plane. Try at most the four targets `+u`, `-u`, `+v`, `-v`, each about **4 m** away and each direction **once**. Poll each attempt to terminal and stop at the first success. If all four fail, stop and inspect again; do not expand into unbounded search.
- A landing capsule is a DSP vegetation (`VegeData`) collision object, not a factory entity. It may be visible through the vegetation resource API, but callers must keep the four-direction fallback because its identity/name is not a general factory-entity guarantee.
- Move is only an approach primitive. If the original business action (for example harvest, transfer, or build) now prepares successfully, perform that bounded business action instead of continuing to drive toward the building or resource center.

All recovery attempts use the existing `prepare_move` / `commit_move` flow. Never teleport, write player position, inject input, replay a failed target, or run an unbounded pathfinder inside the Plugin.

## Construction and production lines

- Use handcrafting for bootstrap buildings and small missing prerequisites, not as a substitute for an unlocked production line. Prefer sustainable upstream automation; while machines or research are running, inspect power and plan the next dependency.
- A successful build commit is not enough to declare construction complete. Fresh-read until the expected prebuilds are gone and construction drones have settled, then inspect the resulting entity, recipe/configuration, power network, and directed belt/sorter endpoints. Do not infer a free endpoint only because expected output is zero.
- Do not declare a production line complete merely because every building exists. Prove that the intended recipe is unlocked and configured, power is served, the intended inputs are consumed or carried, and the expected output increases over a bounded game-tick window. A first-event Journal entry is supporting evidence only when `durableThroughSequence` covers it and persistence is neither pending nor errored.
- After each completed production line or other user-approved milestone, perform a normal owned-world save and verify the saved tick plus healthy write state before moving on.

## Restart and interplanetary flight

- Protected resume and flight-checkpoint capabilities are not save pickers. Use only the currently advertised token for the exact owned world and flow; never preserve or revive an older capability after newer progress.
- Before actual interplanetary flight, ensure core energy and normal fuel satisfy prepare, then let the flight commit create and verify its dedicated checkpoint. Poll through stable landing. Reload that checkpoint only when the matching flight reports `recovery_required` or the documented interrupted-flight condition.
- A failed retry may reload the same matching checkpoint again. After stable arrival, confirm the checkpoint capability disappears and normally save the primary world so retirement is durable before starting unrelated production work.
