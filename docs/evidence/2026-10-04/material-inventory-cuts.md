# Bounded material-inventory cut (offline implementation)

## Direct Gate2 blocker and scope

The existing detail-only `beltCargo` reader counts stacks touching one belt
segment. Adjacent segments can contain the same stack. Summing those readings,
or joining different-tick device reads, cannot establish the complete in-transit
stock needed by the current1210 source/finite-buffer review. Prior continuous
production evidence remains valid only within its declared scope; see the
[stage evidence](../2026-10-02/warper-automatic-source-and-build.md).

The existing `inspect_factory_entity` now accepts optional explicit
`materialInventoryObjectIds`; this is one read-only extension, not a new tool,
traversal, planner or writer. Omitted/empty selections retain the ordinary
single-object read with no extra inventory work. Ownership/local-factory checks
run before selection validation or capture.

## Bounded capture and interpretation

- At most256 unique positive built-object IDs; invalid, duplicate or oversized
  selections return `INVALID_REQUEST`.
- Selected assembler, miner, storage, tank and inserter stocks plus each
  selected belt's **entire native CargoPath**, deduplicated by native path ID.
  Unsupported components (including labs) make the cut unavailable; they are
  not silently counted as empty.
- One Unity-main-thread call with one captured game tick. A tick change rejects
  completeness. This does **not** promise an atomic native transaction.
- At most64 distinct paths,32768 total path cells and4096 total path members;
  existing per-path8192-cell/512-belt native-adapter bounds remain unchanged.
- Reuses `NativeBeltPathCapture`, strict whole-path marker decoding and cargo
  readback validation. Verifies every member's belt/entity/path identity and
  each cargo ID against native `GetCargoAtIndex` before returning counts.
- Full path membership is disclosed, including members outside the explicit
  selection. Never prorate a path, add it once per belt, or stitch different-tick
  cuts into a single inventory. Tank scalar and tank-fluid buffer describe the
  same stock and are not additive.
- Missing object/buffer/pool data, malformed values, unresolved native seams,
  budget rejection or readback mismatch keep whole-cut `state=unavailable`
  with a reason. Partial detail is not a complete or zero cut.

Storage grids are checked for complete native/captured correspondence rather
than accepting skipped malformed slots as zero. Assembler native recipe,
served/produced/inc array shapes are checked. Inserter held stock uses native
`itemCount`, not `stackCount`. Item identities, nonnegative quantities and item
units must be known. Existing action/configuration/endpoint hashes exclude the
optional observation.

## Verification boundary

The initial implementation's focused Release run passed13 Core and3 MCP tests (0 failed/skipped).
The related path/cargo test selection passed159 Core tests; the related entity
guidance selection passed6 MCP tests. These sets overlap and are not additive.
Core and current-DLL Plugin Release builds passed with0 warnings/errors; Plugin
was rebuilt after the final storage/inc-array checks. Streaming Claude review
ended `APPROVE` (`result/success`, no blocking findings). Protected offline
receipt `derived-material-inventory-cut-offline-b69831e5fdea4ecc8ffd6552360223de`
has SHA-256 `F1ECF52779AE2396B18AD1435031526AF08852666C67F1F2829A45A3A2DE8DEB`;
that reviewed native adapter SHA-256 is
`8D1104631C75844CC0FFE34E7FAC26C40409F09FF56F4986740F280BF873C462`.

Direct tests cover selection/path budgets and deduplication, unchanged segment
stack limits, whole-path readback failure/empty distinctions, unchanged existing
action hashes, actual MCP request forwarding/default behavior and embedded
guidance. The Plugin is compiled against current native refs
`0.10.35.29104`, with `Assembly-CSharp.dll` SHA-256
`6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`.

This page records **code and offline verification only**. The change has not
been installed or tested live; the running Plugin/MCP remains cohort `3fe31d1`.
No Game/Bridge calls, deployment, game writes, saves, restarts, accepted resets,
tag, release or publication are part of this implementation evidence. A stock
cut alone proves neither flow nor allocated/continuous supply and does not close
`finiteBufferExclusion`, full-sourceSupply, Governor or Gate2. Any later live
experiment must declare actual cut coverage, sampling/production windows and
failure conditions prospectively.

## Cross-path cargo alias guard (offline follow-up)

Deduplicating native **paths** alone must not accept the same native cargo handle
on two different paths. The bounded cut now also reserves decoded cargo IDs
across all newly captured paths and rejects any shared/invalid handle before
returning an observed cut. Re-selecting a belt on an already captured path still
uses the same path once; it does not reserve the cargo again. Failed reservation
does not partially modify the handle set, and the existing native path stack
bound remains in force.

Three additional Core fixtures cover cross-path duplication, transactional
failure/empty/missing/invalid distinctions and the820/821 bound. The resulting
focused Core suite is16/16; current-DLL Plugin Release rebuild has0 warnings or
errors. This is an offline integrity guard, not evidence of cargo corruption in
the live world. The earlier2548-test `3ac1f6f` cohort remains **uninstalled** and
must not be used as validation of this later code. A fresh same-source cohort is
required before deployment; no new Game actions or Gate credit have occurred.

The combined inventory-cut/native-path Release selection passed92/92 (including
the16 cut fixtures),0 failed/skipped. Protected receipt
`7235b81375474cbf915d79bfbb6421ec:1` has SHA-256
`52B4D81D095C605A2CD36ED7978C5786AA8FC40DAE3DBF4497AA07321A180D41`.
The follow-up streaming review reached terminal **BLOCK**, retained unchanged in
`615328438044433aa44b63e914509ffb:1` / SHA-256
`F568495991DE5122AA9E576BA8205A0B0559F273E11DB8149D5BA3B13D80A2CB`.
Root judged its single finding non-applicable, not an external approval:
`TryLocateAllCargo` already deduplicates legitimate boundary overlaps and rejects
separate-frame aliases; repeated native paths reuse their existing capture;
`TrySummarizeCore` itself rejects duplicate references, contrary to the finding's
premise. No check was relaxed. The independent disposition is
`13a15237c2cc4fcdb909147f9c246d9a:2` / SHA-256
`37E5DBCC6FDBCD7F0258BC870BC1BB9FF89A4D1A68C52B957C54CCB57A6E046E`.
