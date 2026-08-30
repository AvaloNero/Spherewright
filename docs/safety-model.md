# Safety model

- `AllowWrites` defaults to `false`; every write is prepare + idempotent commit + readback.
- Factory/save reads and all writes require a session created by the current Spherewright process. An unrelated session exposes restricted bridge status only.
- M0 new worlds are peaceful, 1x resources, sandbox-disabled, and created through DSP's new-game path. Existing saves are never enumerated, loaded, or read.
- `combatModeStatus` and `sandboxModeStatus` are tri-state. Unknown fails closed; sandbox enabled blocks every M0 commit.
- Computer Use, screenshots-as-state, visual recognition, and keyboard/mouse macros are forbidden for M0 execution and acceptance.
- No action may grant items, inject storage/buffers/energy, write technology progress, directly complete prebuilds, teleport the player, change game speed, or edit a save. Calling DSP's normal save API for the exact current process-owned identity is allowed; enumerating, opening, or accepting the name of any other save is not.
- Refuelling must move already-owned native fuel and prove count plus proliferator-point conservation. Research-result acknowledgement uses the native UI fade/close lifecycle and never changes research state directly.
- Normal actions must prove the same costs the game imposes: range, inventory, capacity, energy, recipe/technology unlock, construction, power, and elapsed game ticks.
- All current DSP/Unity reads and writes are dispatched to Unity's main thread. Pipe workers see only immutable DTOs.
- Plans bind the bridge/session/planet, action type, exact target identity, parameters, before-state hash, and resource budget.
- Commit results are cached by `(sessionId, idempotencyKey)` across Pipe reconnects. Same-key retries replay; different requests conflict.
- If a mutation started but the result cannot be proved as before or expected-after, the session write subsystem is quarantined.
- Tokens, absolute paths, save contents, private game objects, and stack traces are never returned through MCP or written to normal logs.
- The Named Pipe uses a per-process random name, startup-rotated token, and ACL restricted to the current Windows SID.
- The old sandbox basic-line experiment is excluded from M0 Plugin compilation and the public write surface.
