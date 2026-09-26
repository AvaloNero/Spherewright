# Ordinary belt start-input completion proof

## Defect and correction

`ProvesBuiltEntities` previously verified the first ordinary belt's input only when the plan specified a positive source object. A supposedly free start could therefore carry an unexpected input; slots 2 and 3 were not checked either. Output/free-output verification was already present and is unchanged.

The completion check now requires native input slot 1 to match the planned source and direction, or be empty for a free start. Slots 2 and 3 of that same first **new** belt must be empty. Negative/prebuild IDs cannot pass an expected-empty check. Existing source-anchor neighbors remain protected by their original topology proof; this does not forbid the anchor's own valid side inputs or change elevated/blueprint proofs.

## Verification boundary

- Nine new parameterized Core cases, plus the two existing output cases: **11/11** passed.
- Full Release build against the locally configured current game references: **0 warnings, 0 errors**.
- Core/Contracts/MCP Release suite: **2225 passed** (`1984 + 59 + 182`).
- Independent Sol review checked the first-new-belt scope, source-anchor and elevated-path interactions. These are helper tests and source/full-build evidence, not a native Plugin fixture or a new live construction result.
- No new tool, action type, token semantics, material behavior or automatic retry. No cold deployment yet; the running cohort remains `f4fc8e5`.

Remaining engineering findings from the same review are separate: Harvest approach watchdog, bounded action history and transactional package replacement/rollback. This fix does not claim those complete.
