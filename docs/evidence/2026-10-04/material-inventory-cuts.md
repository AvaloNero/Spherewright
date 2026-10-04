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

The latest focused Release run passed13 Core and3 MCP tests (0 failed/skipped).
The related path/cargo test selection passed159 Core tests; the related entity
guidance selection passed6 MCP tests. These sets overlap and are not additive.
Core and current-DLL Plugin Release builds passed with0 warnings/errors; Plugin
was rebuilt after the final storage/inc-array checks. Streaming Claude review
ended `APPROVE` (`result/success`, no blocking findings). Protected offline
receipt `derived-material-inventory-cut-offline-b69831e5fdea4ecc8ffd6552360223de`
has SHA-256 `F1ECF52779AE2396B18AD1435031526AF08852666C67F1F2829A45A3A2DE8DEB`;
the reviewed native adapter SHA-256 is
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
