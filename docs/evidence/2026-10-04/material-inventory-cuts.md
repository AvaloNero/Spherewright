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
The current 120-check result includes those assertions. This offline review did
not claim live validation of the optional sampler field; the later local result
is separated below.

## 2026-10-05: full declared stock-cut preview, not supply acceptance

Source HEAD is `4ac502c`, with the already installed `b1557bb` Plugin/MCP cohort
and native `0.10.35.29104`; this caller-only work required no further deployment.
Steam and DSP stayed in their existing processes. The user-cancelled dedicated
Host-close survival test was not performed. No new game writes, construction,
material transfers, save, exit, or load occurred in these checks.

Path mapping originals `49fa1127a3f44f24b07bff7fbb2a28c0:1–16` contain fourteen
reads, ten cuts, sixty newly observed paths and 1,634 full members, covering the
remaining 1,514 declared candidate belts. Protected elapsed time is
8,702.4752 ms. Independent root proof `0877b77248b947ee96bc77fd374222d5:1`,
SHA-256 `41AE15B4C211D1AFD833E353176E65C46E63D788182A863902D0831EBFB09111`,
checks original hashes, exact selection and native bounds, member uniqueness,
path metadata, owned boundaries and Journal. Together with the prior ten paths,
all seventy relevant paths are assigned to explicit commodity cuts; their
42,758 cells exceed one aggregate native cut, so they are not joined into one
synchronous stock observation.

The fixed optional shared-sampler selections include the existing forty-eight
frontier objects, eight raw sources and all 137 candidate inserters. Each of
twenty-four commodity cuts adds one representative per complete required native
path, including outside-winning-route candidates. The largest cut has 210
explicit objects and 8,244 cells; existing public limits are unchanged.

An initial private call used independent mode with `EntitySampleEvery=2`, which
the unchanged shared caller correctly rejected. Originals
`f4ea74e63ad748f286c4a693bfd164e0:1–4` contain **two** successful outer S/J reads
before that sampler rejection, not zero Bridge requests. No stock cut or game
write occurred. The old declaration was consumed, the private cadence parameter
was corrected to one, and a distinct finite declaration was used. This is a
caller failure, not a native factory rejection or a replayed accepted action.
Offline parameter preparation also exceeded its budget and was interrupted;
this is not evidence of workflow speedup.

Local preview originals `48333a5c8dfb436683dd3e8e9ad2cfd9:1–41` contain 36 native
reads and 24 observed stock cuts. The protected summary time is 42,624.0774 ms;
the caller's later stdout timing was 42,702.5974 ms. Its sampler accounts for
27,886.53 ms of native read wall time and 12,000 ms scheduled waiting. One
600-tick window qualifies, but continuous credit is zero. Root stock-cut proof
`f724d6a44f3247c085fb89d31cce6f12:1`, SHA-256
`C42195D84929536F36C7BF782ACB48941A97D9C7DD30CEA6B4A96AF2018490D9`, independently
checks the exact object sets, units and stock sums, complete path metadata and
same-tick capture **within** every cut. No observed declared commodity was
found outside its assigned path set; that is not proof about unobserved future
traffic. Cut timestamps and stocks are never added across commodities or joined
to the rolling production window as if synchronous.

One static delta remains explicitly unproved: miner 1213's resource list changed
from `36,37,43,35,42` to `36,37,43,35`. All other compared configuration fields
match. The proof retains this exception rather than calling it natural depletion
or complete static acceptance; node 42 absence still requires a bounded native
read. No permission to change the factory follows from this observation.

Latest observed tick is **92676847**, revision **1**, normal saved tick
**92128739**, durable Journal **100**, external accepted **9**, lifetime **149**.
Both workers stopped; their read declarations were consumed. There is no unknown
or in-flight accepted action. Full source allocation, finite-buffer exclusion,
continuous supply, Governor and complete Gate 2 remain **unproved**. The next
step at that point was node 42 reconciliation, then a prospectively declared full-source
experiment, not another lifecycle test, permanent construction or blind replay.
This read-only preparation is not a new milestone commit or a release claim.

### Resource 42 reconciliation and bounded source experiment

The later fixed native read run `b90e6e7f9fb4404f933ae6b7e651a268:1–12`
completed ten reads and zero writes. Node42 returned the predeclared exact
`INVALID_ENTITY` / “The requested resource node no longer exists in the local
factory.” negative. Iron nodes35/36/37/43 still held2120/18993/20706/8006 units
at their own read ticks; miner1213 retained `36,37,43,35`. The protected result
wall was1873.651ms; the caller's later stdout timer was1982.46ms. Independent
root proof `c357c076448e49ceaa26602af4ce0486:1`, SHA-256
`3AF494F9E48580471AAC8F25DE238CFD23620F23EAAAA188C7B8C4752475E43F`,
checks all12 original hashes, exact ten-method order, owned/session/save/R and
unchanged durable Journal entries. Closing observation92784601/R1,
save92128739/J100, external9/lifetime149. Only that resource-list delta is
reconciled; its historical depletion actor is not attributed. The older stock
audit's then-unproved exception remains in its immutable original.

An intervening invocation of the already-consumed preview entry stopped before
any Bridge call at its unused-authority guard. It had zero native reads/writes
and is distinct from the earlier cadence caller error that already read S/J.
Neither is evidence of a native factory rejection or an accepted action.

The next declaration uses the existing shared sampler:24 complete commodity
cuts at the opening and closing only, with24 lighter producer/store observations
in between. It declares390tick intervals,600tick native windows, every-second
entity sampling, at least36000 uninterrupted qualifying ticks,120 samples,
4096 total reads and3600 whole wall seconds (3300 for the continuous substage).
No prior credit is reused. Different cut timestamps are not joined or treated
as rolling flow. Source allocation and finite-buffer exclusion require root's
original-evidence review, not the sampler's `sampling_completed` label. This
experiment ran under original handle8899; its later terminal is below. It does
not authorize construction, material movement,
save/restart, another Gate or the user's cancelled Host-lifetime test.

The original run `f19264fac7ec4b88ad5de631a25c80e3:1–2526` ended normally,
with2396 native reads,120 samples and zero writes. Terminal original SHA-256
`7E05B8244D10726D627F337A97604C5622FCBF379DB1DAB38A970E10E7D41EA6`;
protected whole wall1005274.057ms (later stdout1005360.08ms). Opening/continuous/
closing sampler totals were49973.1001/903840.2579/48868.5901ms. Independent
root proof `c5df85532fc641c6bf85eff3a4d5554a:1`, SHA-256
`57F0FE52E2F278F85480C27E7410372B6F84F94EA822CE75CB30240B3A5A14C6`,
checks all2526 original hashes,48 complete stock cuts, configurations, native
path memberships,60 actual observations per declared entity, power, unchanged
S/J boundaries and exact native interval arithmetic. One root offline parser
error on ordinary inspect's absent optional material cut was corrected using
the same originals; no Game call or accepted action resulted.

Sample54 had a155tick uncovered gap; its two session receipts were12seconds
apart while native ticks advanced720. This records a caller-observation gap,
not a stopped factory or proof of its wall-delay cause. The original120-sample
cap ended the experiment; it was not extended or replayed. Final continuous
credit is27378, below36000. Native equality/union bounds make1210 production
in that credited segment exactly4, consumption0; the declared minimum requires
`ceil(27378/3600)=8`. This is **not_proven**, not a passed lower-rate experiment.

Independent opening/closing inventory cuts retained their own timestamps.
Hydrogen1120 changed10333→10285 over92822712–92874823, deuterium1121
11→5 over92822770–92874892, PC1206 remained3292 at its distinct endpoints,
and1210 changed1913→1920 over92823248–92875465. The seven-stock increase
is not the four-item credited native count, nor synchronized flow. Several
other raw/intermediate stocks also declined; none was ignored as a generic
buffer tolerance. Observed commodity/path scope gaps remained empty, without
a claim about unobserved future traffic.

Recipe40 requires hydrogen10→deuterium5. Collider3073's first/last light
observations had hydrogen8/5 and were idle; strange-matter5326 had PC4/iron4
but deuterium1/9, also idle. Cracker3965 did not work in any of60 observations,
its progress stayed2400000 and graphite output stayed20. These observations
bound the next source investigation, not a completed routing diagnosis:
fresh native reads of3073/3075/3965/4182/4185 only, bracketed by S/J,
maximum9 reads/60seconds/zero writes. No repeat long experiment or expansion
is authorized by this result.

Closing observation92876167/R1, save92128739/J100, external9/lifetime149.
Handle8899 is terminal, its declaration consumed; no unknown, in-flight write
or unsaved accepted action. Source allocation, finite-buffer exclusion and
Gate2 remain false. Steam/DSP were not closed and the cancelled Host-lifetime
test was not performed. This remains one authoritative pending Gate2 phase
document, not a per-read milestone, new release or publication.

The follow-up fixed `-File` read run `adbafb5c18204b75a81e93f9c110f4d8:1–11`
completed9 reads/zero writes in about2779.37ms. Original terminal SHA-256
`AB9E5544F56BCE776EAE92F883C3C00CEBEA1DF0AC1D3A5320F9FE01D4E41713`;
independent root proof `049359d4c4ae4fa1a76c3da1f85dde01:1` / SHA-256
`209BC264814A2C36E57F893504DFB4F96E43E102A53AD6E0AE62C8E9D1EAFF56`.
It checks eleven original hashes, five explicit object identities, unchanged
S/J, filters, directed targets and three reciprocal device/sorter ends:
hydrogen3074→3075→3073, hydrogen3965→4182→4089,
graphite3965→4185→4165. Sorter4185 held one graphite; cracker3965 still
had output20 and was idle. This is not proof of the remaining4165 downstream
route or a uniquely attributed blockage. Collider3073 was working with input0
on this fresh frame; zero input cannot retrospectively mean it stopped.
Latest92960129/R1/save92128739/J100/external9/lifetime149, no in-flight writer.
Only offline tracing of the existing graphite outlet is next; no new long
experiment, source construction or lifetime test follows automatically.

Before this fixed entry, a child `pwsh -Command -` stdin invocation exited0
with no stdout/index; absolute UTC times were not captured. Exit0 alone is not
proof that its requested reads happened. The newest inspected factory receipts
still belonged to the preceding f192 run; no accepted write was possible in the
declared read-only snippet. The fixed file entry is independently evidenced
above. No uncertain commit was reclassified, retried or replayed.

### Existing graphite outlet: bounded trace and current terminal demand

The existing outlet4165 was followed offline using seven explicitly selected
pages of immutable snapshot `183568336ec7495d87fe7c0137873b70`, tick90521995.
The directed/reciprocal trace visits100 objects and reaches generators3058,
3060,3062, assembler3404/r103 and the open belt end3427. Graphite passes
through4190 (4171→4179) and4191 (4173→3375); terminal sorter4403 feeds3058,
3401 feeds3060,3400 feeds3062, and3436 feeds3404. This reuses the old complete
topology, not a claim that100 current objects received fresh native preflight.

Luna executed the separately bounded fixed `-File` entry once:
`14da0b2f85444475a4c3306a7e0503e6:1–11`, terminal SHA-256
`A830B517680291ECC1A8A98C6902F0863B2506478C8B533301797DC2EEF0015C`.
Nine native reads/zero writes, no remaining handle; protected terminal wall
2993.7943ms (stdout elapsed3075.84ms is a distinct later measurement).
The same-tick cut at93030997 selected12 explicit supported objects plus the
three complete native cargo paths. It used the existing caller, transport and
material-cut API; actual `pwsh -File` smoke sent zero Game requests.

Independent root proof `f2cfd9fba1a8462c9b6bbc44fce5f403:1`, SHA-256
`0042CC09FE4A1B3DA0050A6BB03FBDEFB1C48D531BD61E443BCB278B98D5029A`,
checks all eleven original hashes, seven snapshot-page hashes, exact method
scope, unchanged owned/S/J, the selected static configurations, item units,
full-path cargo totals, three graphite-burning generators and current power.
Its offline wall was1055.3298ms; no Game calls or writes.

3965 remains idle atprogress2400000 with graphite output20. Outlet4185,
4190,4191 and three generator-input sorters each hold one graphite. Assembler
3404/r103 is idle with turbo1204=4, magnet1102=6, graphite1109=2 and super
magnetic-ring1205 output10. No missing input is inferred from its older frame.
The native graphite path inventories are92:113,140:77,142:65 items; these are
same-tick stock, not throughput or proof of physical path storage capacity.

Generators3058/3060/3062 use graphite and generate3651/3959/3944 J/t on
their individual fresh frames. N3 is full-served, demand/generated197506 J/t,
capacity1806000 J/t and generatorRatio0.109361 at93031039. Generator
`isWorking=false` is not an authoritative stopped-generator field in the
current reader; joules_per_tick is never fuel inventory. Fuel stock is still
unobserved. Current low load is not spare *rated* budget for new construction.

All three paths92/140/142 are outside the prior graphite selection. The older
70-path cuts remain complete for their declared source-route candidate set,
not all coupled byproducts or every factory graphite stock. Their empty
observed-scope-gap list cannot detect an unselected path. This qualification
does not rewrite the old receipts or turn any experiment into a passed Gate.

This narrows the hydrogen-source investigation to graphite downstream
demand/backpressure, not a broken device-end connection or a powerless
generator. It does not yet prove one sustainable repair or exact current
allocation. Next: use existing topology/runtime recipes to qualify a minimal
graphite destination serving1210, account for hydrogen recycling and actual
consumer demand, then verify the complete budget and hardest interfaces.
No extra tank, blind upstream expansion, repeated long experiment or Game
write follows from this read-only result. Source/finite-buffer/Gate2 remain false.

Closing93031047/R1, normal save92128739/J100, external9/lifetime149 unchanged.
No new accepted, unknown, in-flight or unsaved accepted action. Steam/DSP stay
running; the cancelled special Codex-exit test was not performed. Natural
production after the saved tick is not silently counted as saved progress.

### Deuterium shared-store and fuel-path boundary

The immutable old-factory trace `175e8b8a13af4b618f269ddeec7c9ae2:1` (SHA-256
`40540701ADC2A9400812920A14EBFD155FB936848ECE9D03593138A7D5ED7C76`)
shows `3073→3076→3074` reaching the shared store, with candidate branches via
sorter3405 to fuel-rod assembler3403/r41 and via5736 plus existing transport
to5326. Sorter3405 is not a fuel device. This is static topology from the old
5945-object snapshot, not a fresh whole-path qualification.

The fresh same-tick cut `5ec5e4c6f3d448ca9e68bdc1e6122a47:1–11` completed
9 reads/0 writes in4262.5862ms; terminal SHA-256
`F7D4263F56391B0F37016549FE802212B46FD48252CD56FC397040C0DEDC6A12`.
At tick93285636 it covered24 explicitly selected objects:3073 had hydrogen5
versus the10 threshold;5326 had heavy hydrogen5 versus10;3403 had heavy
hydrogen8 versus20. These endpoints were idle at full power, while r41's
other two inputs were nonzero. This does not establish the exact shared-store
allocation or single-machine cause.

One native600-tick window observed hydrogen P/C=11/6 including recycling,
heavy hydrogen0/0, fuel1802 0/0, and1210 1/0. It is a single window, not sustained
production or proof of net hydrogen supply. Hydrogen-fueled thermal power
generator2516 observed3932J/t, but fuel inventory is unknown; N3 was full-served with capacity
1,806,000J/t and demand196,774J/t. Power output is not a fuel-stock measure.

The first4171→5523 candidate was rejected with
`BUILD_LOCATION_INVALID`, `belt_destination_cover_unsupported`, and
`belt_join_requires_empty_open_independent_path` (`0a6545d62c1a422b84937965b630987a:1–13`, 11 requests/0 writes; terminal SHA-256
`BB355672CE4D6278E50AACD532F02B788558F3D23906BE1E0EC538155FB7D4B3`).
This is not a geometric or distance finding. Native and closing receipts
completed before a caller-side `$Error` summary failure; root verified the
refusal and zero-write result were unchanged, with no replay. Combined proof
`264f2437d0e547c7a9d81d409105b551:1` has SHA-256
`2BC7B8DD130A8E2BE3ADB20128369385AF90ECB11FB3AD3EEC99EA3F51318FB4`.

The second use of the shared three-span qualifier
`54572388f4884d46a81663c78d47892b:1–17` completed8 requests/0 writes. Its A
preview ended at `planned_endpoint_TooSkew:source` with
`nativeCheckPerformed=false`; H/D were not prepared. Terminal SHA-256
`A0333993BC410B3C7332D509DF6EC5D7216C3BFDAB5AF0DF31B15CD23CC668A7`,
entry-to-first-prepare1106.9593ms and total2434.6775ms. Root proof
`61e01870b1b041058289663567ea5512:1`, SHA-256
`D8B5D644538178BE55981B8CEFA4886B772F91D6DDC6255C13319F5608C34FC3`,
verified the method allowlist and preview boundary offline. Protected storage
redacts an empty planToken too, so its placeholder does not establish a token
value; this was not an unknown result. BuildStepPlan.Belt uses
`Maths.SphericalRotation(position,0)`; completed belt rotation is redirected
by native cargo path. The planned source orientation failed qualification;
this neither identifies a Plugin defect nor qualifies another orientation.

All executions are terminal; latest observation93424297/R1, saved
92128739/J100, external9/lifetime149, with no unaccounted accepted action.
Writes remain blocked. Full source allocation, finite-buffer exclusion and
Gate2 remain false. The declared targets (1210≥1/min, deuterium10/min,
hydrogen20/min) are goals, not measured supply; fuel1802's actual demand rate
is unknown. Hydrogen recycling gross output is not net supply. Next is only
root review of shared H/D allocation, actual source budgets and source-end
native direction before considering a minimal repair; neither negative
preview authorizes construction, same-candidate adjustment or replay.

### Corrected endpoint pose, ground caller and closed outer-route negative

Root's earlier planned-quarter inference omitted the inserter destination
180° transform. The exact current DLL plus adapter proof
`35c58918bb8f4bc9aff1cb1dee0f8f7d:1` (SHA-256
`24E919E5B9E138DB0951EBDBFFBA40234431F7A41FA0D0340CBDF0E1608B81A8`)
supersedes that inference; it is not native placement approval. For the
same4171 geometry, source planned3 passes the straight-pair guard, but
`304362e296e04355a312a10ffde471e5:1–17` rejects overlap with existing4190
before native sorter checking. H/D were not prepared. All existing links
remain intact. The incorrect planned1 trial `5e0c864…` is also consumed,
not a repeatable fallback. Guide/contract/MCP-description commit `208a570`
passed127 Core and2 MCP tests; exact-SHA CI `37237567822` is green. No Plugin
behavior was changed or deployed.

Fresh coupled-source task `2509c917…:1–6` stopped after4 reads because
BuildCatalog has planet/revision but no sessionId. Root verified zero writes
and continued only the original unexecuted7-read suffix
`60888eba561c4f549dfefd30b9a183b1:1–9` (terminal SHA-256
`4E86C08F7E88A8889F01379F224B8E76E3DBFD7A85EEC6621469F8330D23311D`).
The same-tick24-object cut at93602045 has3073 hydrogen8,5326 deuterium8 and
3403 deuterium14, below respective10/10/20 thresholds, idle/full-power.
3965/r58 graphite output is20 and idle;3966/r58 is working on its frame.
One600-tick planetary window reports hydrogen8/4, deuterium0/0,
fuel1802 0/0 and1210 0/0. Recycling gross output and a zero short fuel row
do not establish net supply or zero legacy demand. Hydrogen generator2516
observed3425J/t; fuel stock remains unknown. Catalog generator ratings
are capacities, not measured fuel consumption.

Seven-read direct-port task `b68171a…:1–9` closed at93670219; five-read
outer-belt task `302fbd1ff9ff41a8bcd370e24b2deaf6:1–6` closed at93817171,
terminal SHA-256 `3B11D5916D7AA864884C9E04A90ABD5A6AEF5EE6CEDECBCC66D35AAC6EAE3748`.
4169 is a diagonal belt corner;4172 is a straight belt with virtual1 facing
west. Their null virtual occupancy and empty generic buffers do not mean
free ports or empty cargo. Old static topology connects3965's graphite
output via4185 and4165…4169 to4172; that is not fresh allocation proof.
Refinery free slots likewise do not prove an unobstructed NEW approach.

The shared bounded read-only caller had hardcoded elevated mode. Commit
`d7a1c61` reuses existing native_grid, omits ground altitude keys (null is
not0), checks exact routing/layer echoes, and keeps all limits, closure,
token privacy and non-executable semantics.100 qualification checks and95
existing thin-stage checks passed with zero Game calls. The initial
StrictMode missing-property fixture failure was preserved; the corrected
v2 passed. Streaming CC review ended APPROVE/is_error=false (raw stream SHA
`18A1547D298E05AA689B164DBCCBF8691A50321F14FD4DA3440541D9F850C7B8`);
its separate post-terminal formatter failed without rerunning review.
Exact-SHA CI `37240082277` is green. The actual ground parameter entry's
ValidateOnly smoke returned3 spans/12-request cap/zero Game calls. These are
offline/caller validations, not site positives; installed cohort staysb1557bb.

One fixed outer-ground4172→5523 qualification
`7610376eea1d4c17a36678e67146e9c6:1–17` stopped at firstA:
`BUILD_LOCATION_INVALID/belt_path_existing_overlap`, NEW point2/object4029.
No object was created; no endpoint native positive, H/D prepare or commit
followed.8 requests, first-prepare1023.9659ms/total2044.5301ms. Terminal SHA
`35D5EEC0C59C2D94F22251A11BD372143CC954EE4610278E1B73B817BDD94862`.
Root independently checked all17 original ordinals, exact eight-method
allowlist, source hash/directions and ground omission, explicit rejection,
player/inventory/session/save/durable-J closure, preserving every original:
proof `169d6b814871467b9877b9529de7f40b:1`, SHA-256
`98B7974D494C956EB92CE12612B585D0293E05DB876227A997B42A2E348CC92A`,
offline662.1149ms/zero Game calls. Do not replay this site or modify its
coordinates/seed; next work requires a complete route/budget redesign.

Latest observation93835314/R1, normal save92128739/J100,
pending=false/error=null, external9/lifetime149. Every task is terminal,
zero new accepted/unknown/in-flight/unsaved accepted. Natural post-save
production is not saved progress. The whole repair, sustainable source
allocation, finite-buffer exclusion and Gate2 remain false. A parallel
graphite feed does not guarantee exclusive allocation or resolve the
conditional net-hydrogen deficit. No new construction, transfers, crafting,
long experiment, cold deployment, save or lifecycle occurred. Steam/DSP
remain running; the user-cancelled Codex-exit survival test was not done.

## Complete current source cut: oil ceiling and coupled fuel demand

This is a diagnosis within Gate2, not new construction or another Gate.
Source HEAD `3a38857b97781c442e840fa6a1688734cac884c6`; installed
`b1557bb`/native29104 remains unchanged. Existing normal action and protected
read transport were reused. No Plugin field, tool, whitelist or executor
was added. The previous goal turn made progress through committed caller
fixes and a closed native negative; it was not a live wait or a Game pause.

Two fixed read-only tasks completed without caller/native failure:

| Task | Original records | Requests | Protected wall ms | Stock tick / closing tick |
|---|---|---:|---:|---|
| Graphite / legacy fuel allocation | `1eedc59b5e4f4d9da99cf492ddd50eb1:1–16` | 14 | 7533.2553 | 93983615 / 93983770 |
| Crude extractors / local oil catalog | `92aab5623f4c45148410028578b03cec:1–10` | 8 | 3127.3412 | 94031465 / 94031517 |

Their terminals are SHA-256
`57ECEB33984C4D2077E84B823D8E786BBABEF88F24790167CF612B4E200AAD97`
and `AF952F2B97C7EA85625881AEC410691674F818265C7BA1E75FE6C76650AEE1B7`.
Root reused the originals, checked all26 ordinal/hash records, exact
read-only methods/success, owned/native/save/R/J closure, same-tick40/7
object coverage, cargo-path deduplication and both immutable generator
pages. Independent proof `07b9eaf5b4294bc68e07d67a0db31f1d:1`, SHA-256
`C7E541EE00CF0DCC8015B91AB929CFD03BB6BB2553C975A4831104BBAD48B2D5`,
929.2688ms/zero Game calls. No prepare, token, commit or new accepted action.

Original `1eed…:9` has a ready, single600tick native window and complete
current-runtime theoretical coverage for1007: exactly2 producers/2
diagnosed producers, **18.14736032485962 crude/min**. Actual P/C=3/4
(18/24 per minute); this quantized short window is not sustained proof.
The refinery nameplate90 oil/min and hydrogen gross225/min are not
allocatable raw supply. New oil resource qualification remains unproved.

Fresh `92aa…:4–7` identifies129/node538 and2802/node540: both normal2307
extractors, full power, zero mined-output buffer.141/707/3964 have zero or
sub-recipe crude inputs;3083/3084 have zero refined-oil inputs. These are
different capture ticks, not a fabricated shared snapshot. The complete
local1007 catalog contains17 oil nodes,15 unmined; all are outside the
current player's build area. Positive reserves/zero miner count do not
prove terrain, placement, transport, power, materials or native eligibility.

Original `1eed…:6` observes3073 H3<10,5326 D3<10 and3403 D5<20,
all idle/full power;3965 has graphite output20,3966 is working, and5334
has diamond output100. In the600tick planet104 window H P/C=8/15,
D=5/0,1210=1/0. A single buffered warper or deuterium batch is not
source attribution, an adequate rate or a continuous window.

Original `1eed…:7–8` covers all133 generators at93983657 without duplicate
IDs/cursors:3 hydrogen thermal,6 graphite thermal,4 fusion among them.
Hydrogen generation totals9957 J/t, graphite13276 J/t, fusion92176 J/t;
two graphite thermal units have0 J/t in that frame. These are current
generation, never fuel stock, fuel emptiness or stable demand. Reader
`isWorking=false` is not generator-stop evidence.

The exact29104 native `PowerGeneratorComponent.GenEnergyByFuel` was
inspected offline (DLL SHA-256
`6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`;
generated research hash `B4736641…918C6000`, private, not shipped).
Using current generation with runtime fuel profiles/heat, the **conditional
unproliferated generation-equivalent**, not measured stable burn, is:
H4.9785/min, graphite8.850667/min and fuel rods0.553056/min.
Recipe41 would require5.53056 D/min (11.06112 H/min) for that rod rate;
fuel proliferation/residual energy/stock and future demand are unobserved.
Thus old fuel cannot be assigned0 or ignored in a whole-source budget.
Even converting all current crude through16/58, ignoring acid/other oil
consumers and guaranteeing every graphite sink, gives only the favourable
stoichiometric H ceiling27.221040/min. These conditional quantities are
not one sustained mass-balance experiment or permission to expand.

Offline reuse of the old5945-object layout (zero Game calls):
`9970992d0bb6431a9b059b88b3970f68:1` / SHA `019791E5…9D6C3E3CC`
traces3965/3966/3084 and coal5187 graphite to existing thermal/SMA/shared
paths, with coal also feeding5334. `c94f040107f44d9f88f98ae12d250dbd:1`
/ SHA `1A789CDC…85ED06DC` traces141/707 H to old stores165/137,
thermal134/183/2516 and redLab256, not3073. Reverse crude trace
`da97c8da0ddf4c709360e116bff7e579:1` / SHA `93257F7F…2A75178`
identifies129→141/707 and2802→707/3964. Static traces are candidate
topology, not fresh allocation or native positives. Graphene1123 belongs
to869→883; diamond5334/r60 requires graphite1109, not graphene.

Next design must budget raw oil, usable netH, old fuel/D sharing and
actual graphite consumers before another tap qualification. No new source
or route candidate is approved here; no blind retry of4171/4190 or
4172/4029. Whole repair/source allocation/finite-buffer exclusion/Gate2
remain false. Latest94031517/R1, normal save92128739/J100,
pending=false/error=null, external9/lifetime149 unchanged. All handles
terminal, zero unknown/in-flight/unsaved accepted; natural production is
not saved progress. No construction, transfer, craft, long experiment,
save, restart/deployment, Host survival test or publication occurred.
Private entry smoke and independent original audit passed; no unrelated
local full build was repeated. Provider usage and total root/Luna model
cost are unknown; no speedup percentage is claimed. The22-request Game
read cost and0.929s offline audit are measured, not the whole planning wall.

## New-source qualification prerequisites and mining alternative

Source HEAD `00b24b718967f6726616f3e8629420754b44deaf`; its exact-SHA CI `37245274225` succeeded. Installed cohort remains `b1557bb`, native `0.10.35.29104`; no deployment or Host-survival test.

- Fixed read-only run `d34099d62cea44faac4375060b8cd43e:1–7`, terminal SHA-256 `4F36D11046CAE092CCCBAEDBD5E52AD52E66CEA535628854CDCE1F973D8952FF`: five requests (`session/journal/progression/session/journal`), 5077.1538 ms, zero writes, closing session observation `94145982/R1`, normal save `92128739`, durable J100, external accepted9/lifetime149. The closing Journal was captured separately at94145988; these are not a same-tick stock cut. No unknown or in-flight action was introduced.
- Root independently checked all seven original hashes, the exact read-only method sequence, protected identity/version/save/durable brackets, and terminal budget in `8517711e08744dc891d586f07b562582:1`, SHA-256 `05A0561325C4870F911305D46437BF991C683DCE2F0664490F50451E4943AF18`; 745.6298 ms and zero Game calls. No repeated native collection.
- Runtime mining technology3601 is unlocked;3602–3606 are not. 3602 requires1000 each of6001/6002/6003;3603 and3604 also require6004,3605 requires6005,3606 requires6006. Current research queue is empty. The public progression snapshot does not expose the native upgrade multiplier: no researched-speed increase, sufficiency proof, technology selection, or upgraded oil capacity is claimed.
- Current DLL SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`. Exact `BuildTool_Click` decompile SHA-256 `4340BBA2FACD2B082AAAA0AE794B21312BA85776C444669291D93C28ECFD6D85` proves the ordinary oil preview binds a well and snaps its final pose to that well before testing the real player `mecha.buildArea`; out-of-range returns `OutOfReach`. Missing real package/in-hand building stock returns `NotEnoughItem`. Spherewright snapshots real inventory and does not add qualification items on this resource-build path. These are offline native prerequisites, **not a live negative or positive for a new well**.
- Reused player original `1eedc59b…:13` has buildArea80, no2307 in the backpack and no in-hand item. Reused oil catalog `92aab562…:7` has all15 unmined nodes outside the current build area;544 is111.57m away and542 is181.22m away. Neither is a newly approved source or native candidate.
- Reused runtime recipe catalog `1eedc59b…:4`: r14 produces one2307 from steel1103×12, stone1108×12, circuit1301×6 and plasma exciter1401×4; r63 steel is `Handcraft=false`. Historical steel storage792 is only a discovery lead from the immutable5945-entity capture, not fresh inventory, material availability, transfer qualification or authorization.

**Next authority boundary:** complete the1210 source/allocation/material/power/layout budget; do not claim the new-source native condition has passed. A physically reachable, genuinely material-backed preview may require a narrowly authorized qualification-preparation stage (normal save/ten-write audit, bounded ordinary Move and exact one-extractor materials), but none is authorized or executed here. Whole repair remains `executable=false`; no construction, bulk future stock, source manipulation, new long experiment, other Gate or Steam/DSP lifecycle is permitted. Existing completed production entities and failed graphite candidates remain untouched.

Provider-level root/Luna usage is unavailable; no token saving, price or total speed-up percentage is inferred. The five native reads and root audit wall clocks above exclude planning, documentation and Git/CI.

## 2026-10-05: close the ten-accepted-action oil-preparation window

Root independently closed the bounded window from original proof
`54e57431ac4343f89c454f702bbeb195:1`, SHA-256
`A1C8AF4D7077AC7F597E9161ABDC4CA13523053D43E802A1EE9C347E12A762FB`
(zero Game calls). Its ten unique successful actions are indexed by runs
`fc8f0f0e907d4f51a50b6e1ddde97332` (5),
`1193a2656645437d88c6434ba88d079a`,
`a9f30a480263417f97da208330dcd903`,
`5cce6a8ddf754c89a4be6565b7086f3e`,
`920e3b7d46a246dda218fa960fb85b4c`, and
`66ad75c2f8f34e32a9e5be0e400dec14` (1 each). Accepted-action summary
`5babc932811040f3b5b311502f8fb3ef:1`, SHA-256
`14D922CA591DB2F4825FD1377E27F42E0E40B495079658379CEBDDEBF5955C8B`,
confirms 10 unique successes, zero replay/unknown/in-flight, and reconciles
player inventory: Fe `1101` −3, coil `1202` −1, all other item deltas 0.

The tenth action was the ordinary save in run
`66ad75c2f8f34e32a9e5be0e400dec14:7`, completed at tick `94656863`
(about 1.781 s); summary SHA-256
`0C4CD30ED8BDF9D34F844DE05B81BF17BAF5C8AB9455F4CA517596F27D81E178`.
It closed at `R2/J100`, external accepted `10`, lifetime `150`. The later
owned read ended at `94691576/R2`. Save tick `94656863` and durable journal
sequence `100` are separate recorded values; do not treat J100 as the save
tick. Natural production after that save is not saved progress. No unsaved
accepted action remained.

Fresh full capture `dabf773d60f0472baa9d1bc61ec0b806` at factory tick
`94667688` contains 60 pages, 5945 entities, 0 prebuilds, 18 detail records
and 11620 reciprocal edges (17.844 s). Baseline capture
`183568336ec7495d87fe7c0137873b70` is tick `90521995`. The audited factory
structure matches; the only resource-membership delta is the exact subset
removed from node lists: `1213` IDs `35,42`, `1496` IDs `45,49`, and `2440`
ID `163`. Iron-99 evidence `5a9fca41cc3b4c6dbb445f223f883725:3` and
Copper-69 evidence `73feed46141948db85b13d813dcd2de4:1–5` (terminal SHA-256
`C57C7B95D9DF9E1DA19D93543732C4A09160E54BB0C576D04920508CE2273257`)
show those IDs are absent from the matching active-product enumerations.
This exact-subset change is compatible with natural depletion; it does not
prove global nonexistence or the historical cause/time of depletion.

The first read caller expected a top-level `kind` on a list page; that
response contract has no such field. It failed locally after three
successful reads, including the completed Iron slice. After root reviewed
the original Iron evidence, the sole writer executed only the still-unread
Copper and closing session/journal suffix: six reads in the combined
sequence, one caller-shape failure, zero native rejections and zero writes.
Iron was not replayed. This is a caller-formatting boundary, not an unknown
Game outcome.

This window establishes the ten-action/save closure and a fresh structural
snapshot only. It adds no live positive for a new oil source, no complete
1210 source-supply proof, and no continuous-window credit. An earlier
window's theoretical crude-oil ceiling estimate was `18.147360/min`, not a
measured sustained rate; net hydrogen and
legacy-fuel allocation remain unresolved. Node `544` has no native
qualification positive. Failed candidates `4171/4190` and
`4172/4029` must not be replayed. Targets remain 1210 at least 1/min,
hydrogen 20/min and deuterium 10/min; the actual demand of legacy fuel
`1802` is still unknown. Whole-source balance and finite-buffer exclusion
remain false. A narrow qualification-preparation exception exists in
`AGENTS.md`. At the close of this ten-action window, the preparation below
had not started; it was later explicitly authorized and is recorded as a
continuation of this same Gate 2 phase. No construction or restart occurred
in that earlier window; DSP/Steam remain running and the cancelled Host-exit
test was not performed.

### Same Gate 2 phase: bounded oil-source qualification preparation

Root independently closed the preparation originals in
`544b1a8de097400cb44cfb99b651a70c:1`, SHA-256
`0EAD33C880DC641B0364AF7EE424ACFB31C3F4A6CFC7870F13FD0BF49B9B3D7F`.
Original `0f3544…:1–84` accounts for four material gathers and one ordinary
handcraft terminal, with inventory, increment and source configuration
reconciled. The four gathers yielded steel `1103` ×12, magnet `1102` ×12,
copper `1104` ×6 and raw ore `1005` ×24. The one-extractor recipe's recorded
leaf inputs are steel ×12, stone ×12, circuit board ×6, magnet ×16, copper ×8
and raw ore ×24; this preparation did not build the extractor or mine oil.
The handcraft caller once timed out at 60 seconds; the same action's original
receipt `cad23a00d343475eaa6325431e6c08fa:1–4` resolved successfully, with no
resubmission.

The bounded move/save originals are indexed by
`de0e6b06c3e84e978ef56cd01f6673f1:1`, SHA-256
`43E3D2D741A740D72E190C7CA4D5CB774C2BB51575B9D35CFBDFB4295B099885`, over
`2ac6e00bde7048d298dc3846416f517d:1–55`. Its sole native positive is the
prepare at `:54`, SHA-256
`302EFE97AF8E029C0B0773CB10B97E57E79535C2C7377227F7235288C2B5E278`;
prepare qualified only the bounded site, not a built entity or oil output.
The first candidate path stopped with zero commits after three shore-risk
samples (`40538328c781423a974f7127a447cc7e:1–7`). The failed Move receipt
`51d7711d6d764a788236c5d8cabd7663:22/39` records 180 ticks of
`position_stalled`; that accepted failure was not replayed. After root reviewed
the rejection, the sole writer used the other bounded route: two Moves then
completed, with one successful save. The window closed at save tick
`94806691`, `R14/J100`; session and resource observations followed at
`94806694` and `94806698`. These are separate observations, not one same-tick
cut. External accepted is `9`, lifetime `159`; the save covers all nine
accepted actions, with no unknown or in-flight action.

The initial read caller expected a top-level `kind` on a list-page response;
that field is absent. It stopped locally after three successful reads,
including completed Iron. After root reviewed the original Iron evidence, the
sole writer executed only the still-unread Copper and closing session/Journal
suffix. Together the sequence contains six successful reads, one caller-shape
failure, zero native rejections and zero writes; Iron was not replayed. This
was a local response-shape issue, not an unknown Game outcome.

The approved scope prepared one extractor in inventory and completed the
bounded moves; it did not place the extractor, produce oil, restart or deploy.
The broader repair remains `executable=false`. The next unresolved design
question is the complete crude-oil → net-hydrogen → deuterium supply budget,
including sharing with legacy fuel, plus the necessary transport hard
interface. No full-source allocation or sustained supply is established by
this preparation. Provider-level usage remains unknown.

#### Current coupled source and power observations

The current source cut is `6435691f315447698b885df9e03f36ee:1–16`: 43
objects at game tick `94827102`. The 133-generator read is a separate tick
(`94827121`), as is the single 600-tick production point (`94827144`); the
closing session observation is `94827162`. These are not one same-tick cut
or a continuous production window. Root's independent audit is
`008f016c3d284e2183e3d3cf1cbd011c:1`, SHA-256
`C7A1C19019F90075410BC104C0739C5A48938967A7C30518CA00380E504C19C2`.
Save remains tick `94806691/R14/J100`, external accepted `9`, lifetime `159`;
the latest observation `94891447` is not a new save. No accepted action was
added by these reads.

The two existing oil producers' current theoretical ceiling is
`15.744960308/min`; `18.147360/min` is an earlier theoretical estimate, not a
measured sustained rate or the current ceiling. The isolated 600-tick point for `1007` read P/C
`2/0`, not a continuous rate. At `3965/r58`, graphite output is `20`, direct
evidence of an output blockage at that read. Gross hydrogen circulation is
not net hydrogen available to deuterium and legacy fuel.

Two zero-write power reads have narrow scope. `48fbea68a7de453d9b5b5413aca087db:1–5`
shows the same-source coordinate assumption for `2303` is not covered; it is
not a positive oil-site power qualification. `36ae911a24b642498170181a2d9c2069:1–5`
reads N3 budget at the occupied coordinate of existing `5329`, under the
hypothesis of adding one `2303`; that coordinate is site-blocked. This is a
budget read only, not a build/placement positive. Capacity is
`1,878,000 J/t`, reserved `1,804,700 J/t`, export `0`; under an assumed added
load of `4,500 J/t` (`270,000 W`), modeled remaining capacity is
`68,800 J/t`. Combined independent power audit:
`274d14c38f4c49ec88c63d89978622f0:1`, SHA-256
`2753FCCA3DBF014C38AEE099059AF712B02CC1469473C8EA5514A2DB42746384`.
The assumed added unit is not a placement positive, and nameplate/point-in-time
capacity does not prove sustained fuel supply.

Accordingly, the current observations do not close full crude-to-net-H-to-D
allocation or its sharing with legacy fuel. The new well remains unbuilt and
unmined; whole-source allocation, finite-buffer exclusion and Gate 2 remain
false. Do not treat these separate point reads, output buffers or conditional
power arithmetic as stable supply evidence.

#### Downstream native-slot observation

Read-only originals `cd1da6dd64054d75ae05be9d56ffc3c5:1–6` show the native
slot layout of `3964`: physical slots `0/1/4/6/7/8` are empty; existing
connections remain at slot `2 → 4181`, slot `3 ← 4193`, and slot `5 → 4180`.
The separate `3996` detail contains only four virtual poses with `slot=-1`
and `occupied=null`; these do not establish physical-slot availability.
Root's independent audit `c43956056f55496cb2c90a1bed5bad3f:1`, SHA-256
`C61A5FA10F5C1837468765860C299EF486DDD94CC7CABCDFA3EEEDA79FC134A3`, confirms
the `3964` configuration and endpoint hashes match the preceding 43-object
cut. Save remains `94806691/R14/J100`, external accepted `9`, lifetime `159`;
the slot-read bracket's observation was `94891447`, not a new save. The later
prepare-only observation is recorded below. This only identifies downstream
candidates: native placement, a new route and whole-source supply remain
unqualified.

#### Follow-up prepare-only rejection

The later `r8×3` prepare-only request is recorded in original
`7b1fc4836a7c477c8f83b751524042f3:1–7`: seven native requests over
1538.4 ms, with zero commit, ending `ACTION_REJECTED`. Native did not return
an item-level breakdown, so the specific missing-material cause is unknown.
The same observation bracket remained `R14/save94806691/J100`; inventory
count, increments, slot counts, empty hand and empty handcraft queue were
consistent in the read. Latest observed tick was `94930594`. External accepted `9`
and lifetime `159` did not change. Root's independent proof is
`131737cbbb914ec298479c7562536c72:1`, SHA-256
`E942BA4DEF87FA7FAA62F8F9626351041564E96ACF30EE41239BE02340032634`.

Runtime recipe catalog `643569…:4` gives only a paper-input estimate for
three power towers: iron `1101` ×6, magnet `1102` ×4, copper `1104` ×2,
leaving one coil in the batch estimate. This is not a native shortage
diagnosis, inventory reservation or authorization. The prior bounded
qualification-preparation authorization is consumed; additional gathering,
tower handcrafting and short moves remain unapproved. The overall plan stays
`executable=false`.

#### Static crude-to-graphite route reachability and conditional balance

Root's original static trace `f408dec59d4e4c01b7b76c24f7418660:1`, SHA-256
`D01B09616B34444338E1B8B265DDC2E3CE03C038B9100FAFD3024F9EC2BAAB59`, uses
the immutable 5945-entity capture: 60 nonempty pages share snapshot tick
`94667688`; a separate empty 61st response at `94668684` is not part of that
snapshot. The 43-object overlay at tick `94827102` checks identities, items,
recipes, pick/insert/filter and connections only. Its configuration hash
includes dynamic buffers and is not a static-only hash.

The traced routes show refined oil from `3964` outlet `4180` first reaches
`3965/3966` (`r58`). Their graphite outlets `4185/4189` can reach `3404`
(`r103`) and existing thermal generators `3058/3060/3062`; `3083` outlet
`3123` can reach `3059/3061/3063`; `3084` outlet `3122` can reach `870→869`
and the same thermal group. `3404` SMA connects only to fuel-rod assembler
`3403/r41`. These are directed static paths, not evidence of present receipt,
continuous consumption or exclusive allocation; all known thermal branches
are retained in the trace.

Conditional recipe balance: if all refined oil feeds these two `r58`
consumers, with `p` `r16` cycles and `x` `r58` cycles, then `x=2p`, net H is
`p+x=1.5x`, and graphite output is `x`. Supplying only the target 20 H/min
for 1210 would therefore require at least 13.333 graphite/min of sustained
outlet, before adding legacy-fuel or other hydrogen demand. This is a paper
balance, not a measured allocation or passed supply budget. No thermal,
storage or branch modification, new route or further authorization is implied.

#### Final normal save and independently sealed ten-write preparation window

The later explicit human permission allows the normal save/ten-write closure
first, then small qualification materials, normal dependencies for at most
three power towers, and bounded ordinary movement. It does not authorize
permanent construction, old-line modification, an extra extractor, stockpiling,
game shutdown/reload, deployment, or a passed whole-chain plan.

One new save (`fe7d6b5b7dea41e1b17399815ecc3dfd:6-9`) completed the same
action at tick `94993578`; actual R15 and durable J100 come from native
readback, not accepted-count arithmetic. Its summary at ordinal10 has SHA-256
`A47293A3F1649223AEE0CCCFA263CC2D5B1880900134959A8D02E33E8B53BD15`.
The external window is now10 / lifetime160: nine completed actions and one
terminal `position_stalled` (180ticks, same failed target never replayed).
Normal-save wall time was1919.423ms; save availability is not a restart test.

The complete capture `6d905e717f8649578b110c7cb53a2569:1-85` contains60
nonempty immutable pages at tick `94998720`,5945 unique built entities and
11620 directed connection records. The separate prebuild read at ordinal65,
tick `94999401`, returned0 and no next cursor; it is not a 61st immutable
entity page. Eighteen explicit material/production details, player and power
were also captured. N3 served its observed362038J/t in full, capacity1878000J/t;
this point observation does not prove sustainable fuel or supply headroom.
The capture's Journal is ordinal63, not a closing Journal. A separate bounded
closure `50f92ae3fa564ff48b1f2e17b790db4a:1-2` verifies durableJ100 with
no pending/error, then the same healthy owned session at tick `95017029/R15`,
normal-save tick94993578. This supplemental closure performed two reads and
zero writes,865.6ms. No game restart or Host-survival test was performed.

Root reused this capture and the old sealed baseline without new game calls.
The strict shared comparator initially rejected the native DTO's null
`materialInventoryCut`; [IFX-063](../../incident-fix-log.md#ifx-063--工厂审计调用方拒绝共享dto的空库存切片占位)
records the minimal caller fix and21 direct regressions. The replay reports
zero added/removed/static changes/nonreciprocal edges,199 dynamic changes;
it does not ignore populated cuts or unclassified fields. Backpack count/inc/
slot totals and held item remained conserved after the already audited
preparation; the single extractor remains in inventory, not built.

Independent root proof `1566ad31222d485c8db4e5d8c6df3c94:1`, SHA-256
`9840082AFD4241FE8BF04C019F70BA5328172ECFAB7943742F0CEECBD63E3B79`,
binds all ten original commit/terminal pairs, prior material/move proof hashes,
normal save, owned identity, Journal, complete factory/configuration/edges,
inventory and explicit production/power observations. All indexed actions are
terminal, with no unresolved intent/unknown/replay; session DTO itself does
not advertise an in-flight field. Audit-only local processing was69119.065ms,
zero game calls/writes. Source HEAD was`8b86288`, installed cohort remains
`b1557bb`, native`0.10.35.29104`; no binaries were changed or deployed.
Provider-level token usage is unknown. This is not a new-oil output, a complete
1210 automatic source, a continuous-production window, or a restart proof.

External10 remains frozen until the phase commit/push, its exact green CI
and root's explicit new-window handoff. No game revision/tick/Journal/identity
is reset by that external bookkeeping.
