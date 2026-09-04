# Spherewright Agent playbook: opening movement and bounded recovery

Read this playbook before the first gameplay action in a session, especially after creating a new world. It is also available from the MCP resource `spherewright://agent/playbooks/opening-movement-v1`.

## Commit discipline

1. Fresh-read the session and player before prepare. Use the current session, planet, revision, and player state hash.
2. Treat `prepare_move` as target validation, not path clearance. It validates a finite point on the current planet but does not predict collisions along the route.
3. After every `spherewright_commit_*`, poll every returned `actionId` with `spherewright_get_action_result` until `terminal=true`. A commit acceptance is not completion. If a synchronous commit returns no `actionId`, use its documented terminal result and then fresh-read.
4. After a successful Move, fresh-read the player. Continue only after `movementState=Walk`, speed is at most about `0.1 m/s`, and core/fuel energy is adequate for the next leg.

## Stalled Move recovery

- If the terminal result says `failureKind=position_stalled` or `failureKind=route_stalled`, **do not submit the same target again**. Fresh-read before every new candidate.
- If one nearby obstacle is identifiable, project the direction from the obstacle toward the player onto the player's local spherical tangent plane. Prepare/commit one target about **5 m** along that away direction, reprojected to the planet surface.
- If several obstacles are nearby, or the landing capsule/base cannot be identified reliably, form two orthogonal axes on the local tangent plane. Try at most the four targets `+u`, `-u`, `+v`, `-v`, each about **4 m** away and each direction **once**. Poll each attempt to terminal and stop at the first success. If all four fail, stop and inspect again; do not expand into unbounded search.
- A landing capsule is a DSP vegetation (`VegeData`) collision object, not a factory entity. It may be visible through the vegetation resource API, but callers must keep the four-direction fallback because its identity/name is not a general factory-entity guarantee.
- Move is only an approach primitive. If the original business action (for example harvest, transfer, or build) now prepares successfully, perform that bounded business action instead of continuing to drive toward the building or resource center.

All recovery attempts use the existing `prepare_move` / `commit_move` flow. Never teleport, write player position, inject input, replay a failed target, or run an unbounded pathfinder inside the Plugin.
