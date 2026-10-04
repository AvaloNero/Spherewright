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
across newly captured paths and rejects shared/invalid handles before returning
an observed cut. Legitimate overlap at the same packet-window boundary is
allowed; separated-position duplicates and aliases across different native paths
fail closed. Re-selecting a belt on an already captured path reuses that capture,
and failed reservation does not partially modify the handle set. The existing
native path stack bound remains in force.

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
`TryLocateAllCargo` permits legitimate same-packet-window boundary overlap and
rejects separated-position aliases within a path; repeated native paths reuse
their existing capture. `MaterialInventoryCutPolicy` separately rejects a shared
cargo handle across distinct native paths, and `TrySummarizeCore` rejects
duplicate references. No check was relaxed. The independent disposition is
`13a15237c2cc4fcdb909147f9c246d9a:2` / SHA-256
`37E5DBCC6FDBCD7F0258BC870BC1BB9FF89A4D1A68C52B957C54CCB57A6E046E`.

## Same-source cohort and protected recovery

The follow-up source is commit b1557bb91a71a33c535b1521c267da22fa4aa92a;
Windows Core CI run 37206538632 completed successfully. Its clean-commit
offline cohort passed locked restore, Release build, tests and MCP publish:
2551/2551 tests passed, including16 MaterialInventoryCutPolicyTests and3
MaterialInventoryCutGuidanceTests. The verified cohort has228 files (4 Plugin,
224 MCP), version0.4.0.0, 64 tools and1 resource with matching playbook.
Receipt 2ed96cf586834604b91ad1fd066f67a2:1 has SHA-256
11725B3778E2365B9839F8F7CF211E141F95C7E803642EAFC869CBDBAD4066A9. The
cohort was not installed at build time; a later necessary cold transaction
installed and verified all228 files as one cohort, without creating a release.

The latest streamed external review ended BLOCK, not APPROVE; the terminal and
root disposition are retained above. Root found the duplicate premise inapplicable
to valid production inputs: `TryLocateAllCargo` allows same-packet-window edge
overlap and rejects separated-position aliases within one path; an already
captured native path reuses its capture. `MaterialInventoryCutPolicy` separately
rejects shared cargo handles across different paths, while `TrySummarizeCore`
rejects duplicate references. No guard was relaxed.

Before the cold transaction, the owned world was normally saved at tick92128707,
R2/J100, external8/lifetime148. Save proof
4884f66ed2b24118b67b1017bea160f9:1 has SHA-256
8C49140F6E88FB3695272FF85CA239A6AC21A9A1C9DD55B8D84918D6E9094A0A. The
transaction retained Steam, installed the matching228-file cohort, and started
one new DSP instance; transaction proof
f5122df27c444f759438ed269c7434b7:1 has SHA-256
480D7D0902F826BB88D35B0767D3357911407B563F49C5E3FD5890B70388062A. No
dedicated Host-exit survival test was performed.

The protected default-primary resume adopted the same owned world and then
saved at tick92128739/R1/J100, external9/lifetime149. All100 Journal entries,
inventory and in-hand contents were preserved; player displacement was4
micrometres, within the existing1 mm tolerance. The resume preparation's
exactEmbeddedIdentityVerified was false, so identity is claimed only from
native terminal adoption/readback, not as a pre-load readback. Resume proof
f3fb9acbed464e9d8a717858c3f68014:1 has SHA-256
B496400F9ECEDB4DEA5934072D19398774F4674532A44453153DC09F6DB2968A. The
recovery took about9.5 seconds; the preceding normal save took about2.73
seconds. No unknown or in-flight action remained.

The earlier recovery/sampling-stage factory snapshot contained5945 built objects,
0 prebuilds,17 details and11620 reciprocal edges, with no entity additions or deletions.
It predates the later save at tick92128739 and is not a fresh post-resume capture.
Only the already verified exhaustion of node168 at object2440 is an allowed
static difference; the1535/2317 2011-to-2012 upgrades were preserved and1512
was already2012 at baseline. Full snapshot proof
d7fc1a41af3a4e67a06e5e259c618d34:1 has SHA-256
64D0B819291E6E69BEED440DACD10D8443C9812A9C974878B13E60B7C7105B0C.

The earlier recovery/sampling-stage six non-overlapping600-tick observations recorded
1210 production P/C as
1/0, then0/0 in each of the remaining five windows; combined native output was
one item. coveredContinuousTicks remained0. Inventory changed1011→1013, but
unobserved production between windows prevents attributing that increase to the
six windows. These windows predate the later tick92128739 save/resume and are not
new post-resume sampling. N3 was fully served in each window, with observed capacity
1878000–1914000 J/t against declared peak1804700 J/t. These short windows do
not establish a long-duration credit or finite-buffer exclusion. Original
window receipt c2c6ae9c3199460380d5b6a359ac79f4:1–143 and independent audit
45a1756ad0844cb2b93e172832a77837:1 / SHA-256
AF988AC6A02B44EA05AA32C705793F609A35B173A6487AC0F0A4E465F2443713 preserve
the exact disjoint ranges and counters.

The prior minimum-source proof and material union intervals remain limited to
their declared scopes. Full source allocation, finite-buffer exclusion,
Governor and complete Gate2 are still false. The approved 14-read, zero-write
material-cut observation completed and was independently verified by root:
run `095da7e9b72e4c1ebb14d989c3b16093`, elapsed 7.6554198 seconds; proof
`7430a5fd350347cfb15d3bc9b98d34c5:1` / SHA-256
`C725958C50E94FF46E2551FE8809A11FFF4D3F74A481B98EB823DF66E66BDE3F`.
All seven cuts were observed; members within each cut share a tick, but different
cuts must not be stitched or summed. These are point stock/path observations, not
production or flow measurements:

| Item | Stock | Selected | Paths | Members | Cells |
|---|---:|---:|---:|---:|---:|
| 1209 | 0 | 12 | 1 | 8 | 153 |
| 1112 | 118 | 9 | 1 | 5 | 94 |
| 1127 | 0 | 55 | 1 | 51 | 1092 |
| 1206 | 3264 | 138 | 1 | 132 | 2761 |
| 1121 | 10 | 104 | 1 | 98 | 2063 |
| 1101 | 3446 | 180 | 4 | 166 | 3382 |
| 1210 | 1762 | 33 | 1 | 29 | 594 |

The default single-entity observation and duplicate/257-ID `INVALID_REQUEST`
negative cases passed. Latest observation is `S92211338/R1`; the last normal
save remains tick92128739/J100, external9/lifetime149. There are no unknown or
in-flight actions and no new save. This point read does not establish full-source
allocation, sustained supply, or finite-buffer exclusion. The next bounded work
is to plan source/finite-buffer checks using the validated native paths; it is not
a reason to merge stocks across cuts. Game writes remain blocked pending that
source evidence.

## Optional cuts on existing sampling reads (offline caller validation)

`MaterialInventorySelections` optionally attaches fixed material-cut IDs to the
existing `inspect_factory_entity` payload. With no cuts declared, the default
payload is unchanged; declared cuts add no native requests. Each cut retains its
own `capturedAtGameTick` and is checked against its enclosing entity read, not
the production-window timestamp. Missing, unavailable or mismatched cut data
fails the sample and preserves its reason; the caller does not retry, count
production or grant acceptance credit.

The actual PowerShell 7 `-File scripts/test-production-sampling.ps1` fixture
passed 120/120 assertions with `gameCalls=0`; both changed scripts parsed without
AST errors and `git diff --check` passed. This is offline caller evidence only,
not a live inventory observation. An earlier fixture invocation stopped at parse
time because a hashtable literal repeated normalized key `3404`; root changed
that alias case to typed `Hashtable.Add` construction. No fixture or Game/Bridge
request ran in that failed invocation.

The first non-interactive CC invocation exited 1 without a terminal; its specific
CLI error was not retained and it is not a review result. A later invocation with
corrected prompt/variadic-option placement reached terminal `APPROVE`,
`is_error=false`, session `6fbaf0d0-81f6-406c-babc-c6a671359379`; one
`unrecognized_model` stream event was nonterminal and consumed. Protected full
stdout/stderr receipt `derived-cc-material-cut-e83e2184e8514e98b2be4af7fafd87ee.json`
has SHA-256 `9D144894052DAD65EB8E639DAB2340CBBD883A0C6F7AB84E23BEEAA0F13D39BD`.
That review preceded two final fixture-only assertions (coverage-token rejection)
and the message-spacing correction; the runtime implementation did not change.
The current 120-check result includes those assertions. Nothing here claims live
validation of the optional sampler field.
