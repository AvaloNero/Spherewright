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

#### Raised lateral qualification and current whole-chain budget

The next candidate was redesigned against the complete 5945-entity snapshot,
not offset from the rejected descent. An offline ground continuation would
cross 4215 and approach 3402; it was rejected without a Game request. The
chosen elevated lateral span crosses both retained rows and the return column
before descending on the consumer side. Static advisory `466c86…:1` is not
native placement evidence.

Run `c898d01722714dd7aa2557861c7e0d91` made 16 native reads in 3784 ms:
544/yaw180 placement at ordinal5, uphill 21 points at7, elevated lateral
17 points at9, downhill 7 points at11, and consumer-inlet preview at14 all
passed. The latter is tokenless, `prepared=false`, `commitAllowedNow=false`:
3964.slot1, filter1007, quarter-turn3, five planned points and one 2011
preview. Independent root proof `2ad9ec2d6cc34d68a1d20d7e24e5c041:1`, SHA-256
`AC48955F528285441639148FC855555B33F98B53D70102B9C4EB212EC877BB5D`,
checks all original reads and unchanged inventory. Intentional future-join
gaps remain between the separately qualified empty spans. No endpoint was
cropped, no actual-ID join or complete joined-site preflight was claimed,
and no prepare token was persisted or used.

The following current source/material budget reused the existing coupled
read-only caller, with the declared accepted value corrected to7 rather than
the historical9 in its intent/summary. Run
`763db041702e456c93d5beb2ac015428:1–16` made 14 native reads in 9853 ms:
S/J, runtime recipe/build catalogs, one explicit 46-object material cut, two
immutable generator pages, production/power, the two retained oil nodes,
player and closing S/J. Root proof `356330415261450086cfde9513c7dd46:1`,
SHA-256 `CBB0A50CE5CA84455BD4B8986CA09C050D400D6CB853AB9463716DE8483414F5`,
checks exact ordinal/method/hash coverage, all selected same-tick objects,
units, 133 unique generators, recipes and backpack/held-item continuity.

The 46 selected objects have zero static-field differences against the
complete snapshot at94998720; 40 dynamic field differences are retained.
An initial offline root assertion incorrectly used `FactoryConfiguration`
as a static-only hash. Source shows it includes buffers; 11 such hashes
changed during production. Reusing the existing classified static/dynamic
evidence fields corrected that caller false blocker without ignoring fields,
changing Plugin semantics, repeating a Game read or claiming a current full
factory audit.

Whole source–route–endpoint proposal, **not executable authorization**:

| Portion | Sources, direction and consumer | Budget/qualification boundary |
| --- | --- | --- |
| 1127 inputs | 883/r99 produces1206 from1204+1104+1123; 1500/r1→1511 supplies1101; 3073/r40→3076→3074→5736/5735 supplies1121 to5326/r104 | Existing item-aware eleven-pair static proof retained; selected current input buffers1206=4/iron=4/D=2. Preserve3074 outputs3081/3075/3405; no exclusive allocation claim. |
| Diamond | 5187/r17 coal→1109 through the existing route to5334/r60→1112 | Current graphite output100 and Diamond output100 are finite buffers. Runtime1123 is **graphene**, not Diamond feed; 869/r31→883 is its automatic role. |
| Lens and1210 | 5326/r104→1127 plus5334/r60→1112→5333/r101→1209→5329/r78→5364→ordered29belts→5365→5331 | Retained reciprocal source/sink paths, recipes and filtered sorters are not rebuilt. Final stock2249 does not prove fresh or continuous1210 production. |
| New crude | 544→new2307 actual outlet→short empty stub→2011/filter1007→northern ground spans→qualified raised lateral spans→consumer preview→2011/filter1007→3964.slot1 | Preserve old4193 and all3964 outputs. Join independent empty spans before admitting cargo. Actual source ID/port, shortened-ground full native prepares and all same-shell covers are fresh post-build gates, not already passed. |
| Materials/site | Forecast ceiling2307×1/2201×3/2001×164/2011×2; backpack1/3/28/7 | Short136 belts: recipe84×46 yields138; gear recipe5×46 plus direct iron costs138 iron. Backpack37 + additional101 from1511 current3000 can fund the forecast. Nothing transferred/crafted; exact NEW join bill/reservation and complete site are unproved. |
| Power/logistics | Three previously qualified pole sites extend existing N3; source and two2011 estimated14600 J/t | N3 capacity1878000, retained full-base reserve1804700, conditional headroom58700. Individual pole positives are not joint-network/fuel-continuity proof. No towers or new types. |
| Production/allocation | Isolated1210≥1/min needs1206×2, iron×2, D×10, Diamond×4, Lens×1; underlying netH≥20/G≥10/acid≥2 per minute | r58 gross3out/2in means net1, not3. One r16 plus two r58 conditionally yields at most netH45/G30 from raw30/min, only with real drains/inputs. Legacy fuel demand remains unknown; rated upper and point0/0 cannot be used as actual demand. |

Current stock point at95435529: 3073/r40 input H7 (batch requires10),
output D0; 5326/r104 input D2 (requires10), output1127=0; 3074 empty;
3965/r58 graphite output20; 870 graphite1278/acid100; 883 output1206=10.
The separate production point95434984–95435583 (600 ticks) records
P/C1007=4/2, 1109=5/3, 1114=4/3, 1120=5/7, and
1121/1123/1127/1205/1210/1802=0/0. These do not establish source attribution,
steady fuel demand, buffer exclusion or sustained supply. N3 point
195827/195827 J/t, ratio1, is not a fuel-stock or continuous-power observation.

Both new read-only runs are consumed: zero accepted/commits/writes, no
unknown/in-flight/unreconciled action; root made zero Game calls. Source
HEAD `1672aeea47a63aa075ec0f204a14adb7ab83e700`, installed cohort `b1557bb`,
native29104, 64 tools/1 resource unchanged. Latest closed observation
95435606/R24; normal save95141009/J100, external7/lifetime167 unchanged.
Natural post-save production is not saved progress; no restart was performed.

Execution remains `false`: future source outlet/joins and netH/graphite/old-fuel
allocation are open. The preparation-only exception is consumed. A new human
decision was requested only for three already-qualified poles and one
extractor using existing backpack stock, respecting ten-write windows; it
has not been confirmed or executed. No new trunk, bulk procurement, old-line
edits, lifecycle test, save load or other Gate follows from this evidence.
After any separately authorized build, require actual source working/output,
3964 raw receipt, H→D→1127→Lens→fresh1210; then three disjoint600-tick startup
windows, normalSave, protectedrestart and renewed output. Joint36000-tick
acceptance remains deferred until that Gate end; partial/finite-stock output
cannot substitute. Caller/model usage and comparable end-to-end timing are
unknown; the two native wall times are not a speedup claim.

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

#### Three-tower qualification materials prepared and normally saved

The prior closure was pushed as `a5da481`; exact-head CI `37281844106`
succeeded before root reopened the external window at0 / lifetime160.
The first bounded Move (`61723f860d1d4fa3804e65ee1df0fa83:17`)
terminated `position_stalled` after180ticks. It counted once and was never
replayed. Fresh native business prepare already admitted the copper transfer
at75.042m within the80m build area, so further movement was unnecessary.

Remaining-only run `3b96dd5c0fe64ca2978cf9044d207322:1-42` took16519ms:
copper2 transfer, magnet4 transfer, normal `r8×3` handcraft and normal save.
All four new unique actions completed, with no additional Move or construction.
Native transfer terminals respectively show copper1900→1898 and magnets
1919→1915; a later copper point read was1899, not1898. That +1 non-atomic
residual is recorded separately and not attributed or treated as transfer loss.
Backpack readbacks exactly conserve count/inc and held item: craft consumed
iron6/magnets4/copper2, produced towers3/coil1, and preserved extractor1.
The final backpack has iron37, towers3, coil1 and the original extractor1.

Normal save tick95086680 covers external5 / lifetime165; actual closing
session is95086683/R21, durableJ100 with no pending/error. R21 is readback,
not an assumption that revision increments once per accepted action.
Summary ordinal42 SHA-256 is
`85B73E75172B82B0E5BCA34203F182607BBB01BD3A27D1A5BD148AE3D5A8C0EB`.
Root independent proof `aff4829868814969bdc093d4bf8be0dd:1`, SHA-256
`4DA0A13AEDE597B83C4ABCE7AF6D4E6322B76D3F319855DD5612F66FF6424CAF`,
checks all original coverage, same-action terminals, both exact native transfer
debits, all backpack count/inc/held deltas, source configuration, owned/save
and durable Journal. Its final replay took1480.616ms and made zero game calls.
The action ledger has no unknown, unresolved intent or in-flight action.
No new full-factory snapshot, restart, oil output or full1210 source proof is
claimed. Source HEAD is `a5da481`, installed cohort remains `b1557bb`.
Only bounded hard-interface read-only qualification may follow; all executed
entries are consumed. Permanent construction remains blocked while the
whole-chain plan is false. DSP and Steam remain running.

#### Individual power-tower prepare-only site checks

The first bounded caller run `52102b71392b4cb5b8b3d56aeddf7dbc` made eight
native requests and stopped before request nine. The first tower's native
prepare succeeded, but the local summary looked for `commitAllowed` instead
of the response field `commitAllowedNow`; this produced zero accepted actions
and no write. Root's correction proof is
`04eaa79391904ddf9a63bd6df1e3e4d4:1`, SHA-256
`0CC0DCE92F8346170B8B773C1BBDC2CF09DDA3C8663C511AA412D18CCC398B48`.

The remaining candidate suffix `90ec38984e044c128cfb74fee0e559e7:1–14`
made thirteen native requests in 2497 ms with zero writes; its summary SHA-256
is `E9628DCB704396D6BC52892B7E2F158DAD2370761C7C83226FD6FEC5B29556E4`.
Root's independent complete-original audit is
`40968af78baa43dcb70401ffcc20ccdc:1`, SHA-256
`950FA5D322245A0ED85131D5D2C3BCCE33B615E00B3B1EF553C8628043F32BA8`.
The first site's saved snapped geometry was reused, never its short-lived
prepare token. The three adjacent snapped pole spans measured 20.713,
20.996 and 20.645 m; the final pole was 7.875 m from the already-qualified
oil-source placement. Native readback reported a 22.5 m connection range,
10.5 m coverage and a 0.1 m remaining margin.

These are three individual native prepare positives only. No tower was built;
there was no joint native network qualification, material reservation,
operational power or continuous-fuel proof, and no oil-flow evidence. The
external count remains 5 / lifetime 165 and no accepted action was added.
Latest observation is `95122753/R21`; the normal-save boundary remains
`95086680/J100`. The whole-chain plan remains `executable=false`; the next
blocker is the complete oil route into existing `3964` plus net-hydrogen and
legacy-fuel allocation.

#### Approach, source/consumer readback and normal save

The subsequent bounded run `3a097d2024ee4ce3a9e798fc6d9f1f17:1–25`
completed in 9301.593 ms. Its summary at ordinal25 has SHA-256
`6DD032650A3CF4D93884FC2A0E4CD107E2B6F9D1FAEEB6126F0C9C55521E49AD`.
The two new unique accepted actions were the 8 m Move
`74a3fe55-f1ea-4893-8e58-c82732fd929e` and normal Save
`222b6cbe-c278-4b2b-9618-06b2eca7ccfe`; both reached terminal. Root's full
original audit is `dd6d4f0c6f514cd6bfa43ba5afef90d8:1`, SHA-256
`10A7A7DFBD83EC582927D15E78015A3065D973945C601E8537ECF93A63C293C4`.
It verifies the seven unique accepted actions as terminal with no replay or
unknown; inventory count/increments and held item were conserved.

Normal-save tick is `95141009`, session readback `R24/J100`; latest observed
tick is `95141011`. External accepted is `7`, lifetime `167`. No entity was
built and there was no restart. Node `544` remains unmined but is within the
actual build area; its fresh `2307` placement had one native prepare positive
(original ordinal17). This is placement qualification only, not a built
extractor or oil output.

At existing consumer `3964`, the point read was 78.674 m from that candidate;
it is recipe `16`, with raw-oil `1007` input count1, hydrogen `1120` and
refined-oil `1114` output count0, and `isWorking=false`. Physical slots
`0/1/4/6/7/8` were empty; existing connections on `2/3/5` were unchanged.
These are separate point observations, not proof of delivered oil or a
continuous source. The later consumer-side planned-path preview remained
tokenless and non-mutating; its native route check is detailed below, but no
route or entity was committed. The full oil → net-H → deuterium / legacy-fuel
budget remains unresolved.

#### Tokenless consumer-inlet planned-path preview

The first preview `c073d53bc7524cc7b97cf66ffe2dab8a:1–7` stopped with
`prepared=false`, `commitAllowedNow=false` and `nativeCheckPerformed=false`;
the local planned endpoint was `planned_endpoint_TooSkew:destination`. It had
six planned belts and one sorter, zero accepted actions and zero writes. Root's
geometry diagnosis `0b35f15dd4ae4ee7ae12a30b3431141e:1`, SHA-256
`CB9BE4BE1597ACBCFBA39A3082A2AFC06BF677362444954386420C99B4440CD8`, confirms
the current DLL's `Maths.SphericalRotation(position,0)` is local-north and
independent of path direction: quarter1 faced east while this endpoint needed
west (quarter3). The last cell was skewed; this was not a native rejection.

The corrected tokenless preview `bbc7e9871d8540b69cb3d0cbcaf22fed:1–10`
made nine native reads in 1.7865804 s; summary SHA-256
`22DA9BD64C72DAE55F36FF63DF0581802D61E8C1AADD8D049AD3C8C3872B96AD`.
It reused returned native snapped points 0 and 4, not seed geometry, and did
not relax the 11-degree limit. The planned route contains five belts, one
ordinary sorter and span2; the consumer is `3964.slot1`, filter `1007`,
quarter3. The planned-path native check returned `nativeCheckPerformed=true`,
`nativeCheckPassed=true`, `Ok`. It remained a preview: `prepared=false`,
`commitAllowedNow=false`, no token, zero accepted actions and zero writes.

Root's original-by-original proof is
`d08d53ec9ac2443798523cf4a26a0fdb:1`, SHA-256
`81F3DBEDCC61CE99F679C269011969F647F355B1BD396D116A6C6DF60FD980F0`; it
verified the native points and route response, conserved inventory, and
continuous session/save/Journal readbacks with zero game calls. At that
preview's readback, observation was `95176349/R24`; normal save
`95141009/R24/J100`, external accepted `7`, lifetime `167` did not change. The offline caller smoke is bound
to script SHA-256
`F644266ABB2F764A3302675D3845079A5A3831EC49F6A42B9C1DB87F9CD5C75B` and made
zero game calls. A prior local audit mistook a protected redaction placeholder
for an issued token; the control fields and `PlanToken=string.Empty` show this
was tokenless. That caller/audit error caused no game retry or write.

This qualifies only the five-object planned consumer-inlet geometry, not the
source outlet, intermediate crossings or complete source-to-consumer route.
It does not build belts, prove a
connected live route, reserve materials, mine node `544`, deliver oil, or
close the complete net-hydrogen/legacy-fuel budget. The whole-chain plan
remains `executable=false`.

#### Latest bounded production point and static-route projection

The production record `1c06ad00…:3` is a single 600-tick window at
`95216919–95217518`: `1007` P/C `2/4`, `1109` `1/1`, `1114` `2/1`,
`1120` `4/4`, `1121` `0/0`, `1210` `0/0` and `1802` `0/0`. The theoretical
upper bound from the current native components of the two existing oil
extractors is `14.807759761810303/min` (`current_runtime_component_formula_v1`),
not a conversion of the window count. The actual count conversion is
production/consumption `12/24 per minute`; neither proves a continuous rate.
`3965/r58` graphite output remained `20`. Four fusion-buffer
fields are in `joules_per_tick`; do not sum them as fuel inventory. N3 read
required/served `342511 J/t`, capacity `1878000 J/t`, ratio `1`, a point value
not sustained-power evidence. Root's audit of the 25 originals is
`30160eb93cc74498ad3920cbefea2215:1`, SHA-256
`8A209BF70F524B30BB09E6AED0756BE2B72A73F06BC9D01FB906622E67DC36A9`.

Two caller-side read issues are not Game failures: `d474…:21` supplied
`limit=100` where the API maximum is16 and was rejected; after using an
accepted limit, the caller incorrectly required production IDs in request
order. The production record was already persisted and audited; only its
session/Journal closure was supplemented from `5e0fc…`. There was no Game
write or replay.

Static reachability comes from the immutable 60-page, 5945-entity,
11620-connection snapshot at tick `94998720`, not from a fresh dynamic graph.
The first ten pair checks are indexed by
`49dc7920e8c04cb1b7045ff4e4119cda:1`, SHA-256
`68ECEF413C07AF399F647C82F695A89F9B22C9A906D366D86FC3C5E1FFA88FDF`; the
final projection is `e5d1eabf6fe24d6bb54072a27572c32e:1`, SHA-256
`E07825F15FA50CA310F3A03FD5C58034F3E925C73C456794C42298547219DB9E`. All
eleven distinct source-to-destination pairs are statically reachable; the
last/output pair is `5329→5364→ordered29belts→5365→5331`. `5330` is power item `2201`, and `5331`
is storage item `2101`. This does not prove current receipt, allocation or
flow. The historical 571-belt construction forecast was already completed;
it is not new work or a redo target.

The `bbc7…` planned-path preview remains inlet-only. The actual `544` source
outlet and crossing are not qualified; full source supply, net hydrogen and
legacy-fuel allocation remain open. At the final readback, observation was
`95270864/R24`; normal save remained `95141009/J100`, external accepted `7`,
lifetime `167`. No accepted action, construction or restart was added.

#### Prepare-only double-ramp qualification close

The bounded qualification window made 21 native reads plus two stop records,
including 6 prepare checks: four positive segments and two overlap refusals. Run
`1b74b668cfb84182a295622b83c32342` passed three `native_grid` spans of 26/24/24
points; planned point 23 of its fourth segment overlapped existing belt 4031.
After an overall redesign rather than a seed nudge, run
`87da5e34aa1c45e7a526a5c1201242a4` passed the 24-point uphill segment (0→1)
and refused planned point 3 of the downhill segment (1→0) where it overlapped
old belt 4034.
The first close read at tick `95257031` observed existing 2001 belt 4031 and
its retained `4032→4031→4030` line. Belt 4034's position and
`4035→4034→4033` line are from the prior snapshot, not fresh reads. The
21-read audit is `5e0d63f8a3a74b79afaaa92e4aded8ab:1`, SHA-256
`D672F72752AA539430A17B881678F29DAE22E607773661727F1DD16C59972C95`; final
session/Journal closure observed `95270864/R24` with save `95141009/J100`,
external accepted `7`, lifetime `167`.

These were prepare-only segment results: no actual-ID join from the 544 source
outlet to the consumer, complete joined-site qualification, material
reservation, construction, oil flow or sustained supply was demonstrated.
The two same-class overlap refusals are the stop condition: do not try nearby
coordinates, a fifth segment or a downgraded route. Root will redesign the
source-route-end path as a whole; this does not reopen the old 11-segment
chain. Overall executability remains `false`.

#### Oil-source and crossing qualification continuation

Root audited 33 native reads plus one stop record for this continuation; the
consolidated proof is `3cdadd9b272742ffbd981a65772303e7:1`, SHA-256
`4EA7DF7E23F9CCD2CCD5D0D24B3E3B50F67F9E4FE25BCCAB73514E368BC16478`.
Component indices are `56d2d10931e046b6a7c315d4ff297555:1–13` (oil-node and
graphite-sink observations), `49443087a9b6474da00f01e0101647d7:1–6`
(3600-tick fuel-budget point), `8826571093b84d33a096032a4c47d684:1–9`
plus stop `:10` (placement and belt qualification), and
`a349cc5500b0408fae1711b966bbe222:1–5` (session/save/Journal closure).
There were zero Game calls by root, writes, commits or accepted actions; final
observation was `95379077/R24`, normal save `95141009/J100`, external accepted
`7`, lifetime `167`.

The `544` / yaw-180 oil-source placement was a prepare positive at ordinal5;
the 21-point uphill span (0→1) was positive at ordinal7. The downhill prepare
at ordinal9 stopped on planned point4 with `BUILD_LOCATION_INVALID` against
existing belt `4098`; neither the middle nor tail span was called. Fresh
direction is `4099→4098→4097` (an earlier caller summary had it reversed).
Only the 21-cell segment's cargo was empty; this says nothing about the full
existing path. The resource-node list contains 17 entries (`532–548`, including
`544`); `544` amount `71491` and miner `0` apply only to this current build
scope. The other 16 are not treated as current same-scope substitutes; their
mining status is not inferred. No actual-ID source-to-consumer join, material
reservation, build or oil flow was proven.

The production read `49443087a9b6474da00f01e0101647d7:1–6` covers one
3600-tick point window `95342323–95345922`: fuel rods `1802` P/C `0/0`,
deuterium `1121` `5/0`. This is not a long-window fuel gate or a locked
baseline, and does not establish zero legacy-fuel demand. At separate point
reads, `870` held graphite `1490`/acid `100`, relevant `3074` buffers were
empty, and `3965/r58` graphite output was `20`; none is continuous-rate
evidence. The oil-source/sink read was
`56d2d10931e046b6a7c315d4ff297555:1–13`. These point observations do not
close net-hydrogen, graphite or old-fuel allocation.

The remaining design summary preserves the existing 11 source-to-endpoint
pairs. The proposed new route is `544 → new extractor actual output → short
belt stub → sorter 2011/filter 1007 → empty new trunk → 3964.slot1/filter
1007`; join each empty span before adding cargo and keep existing H/D branches.
This is a forecast, not a native budget or authorization: ceiling extractor1,
poles3, belts164, sorters2; current stock is `1/3/28/7`. The `1210` target
requires at least net H `20/min`, graphite `10/min` and acid `2/min`; r58
gross cycle hydrogen is not net supply and legacy-fuel demand remains unknown.
Estimated new extractor plus two sorters load is `14600 J/t`; under the stated
conditional reserve, headroom is `58700 J/t`, not proof of sustained power.
The first direct blocker is the unqualified complete crossing corridor. Net
H/G/legacy-fuel allocation, a future actual-ID join and exact material
accounting remain open. No nearby-seed retry, permanent construction, bulk
material collection or long-window test is authorized by this result; overall
`executable=false`.

#### 2026-10-06 two-pole source-qualification window close

Under the user's narrow source-qualification exception, two `2201` towers
were built from backpack stock through separate native actions. They reached
successful terminals at game ticks `96559201` and `96576481` (306 and 520
execution ticks). Backpack `2201` changed `3→1`; other inventory, increments
and held items were unchanged. The N3 node count changed `216→218`; the native
power table and measured anchors support the new nodes' N3 placement. The
power-node DTO does not expose its network ID, so its null field is not evidence
of disconnection. Root's independent two-tower proof is
`79352a64f8314981b3498440bd814d2f:1`, SHA-256
`98275CB991F393C071DC2224302B1A5E481392807B1EE4416A5E665C2E1DE619`.

The first tower's read timed out, then the same action was queried to its
successful terminal; it was not replayed. After the second successful terminal,
the player snapshot showed construction drones `working=1` and
`pendingBuildTargets=0` (returning). Existing terminal, entity, material and
connection checks were satisfied. Existing playbook guidance says ordinary
next fresh preparation and save do not require all drones to be idle; this was
not an action failure or a missing shared API. The separate initial save
attempt used the wrong player-state hash and returned `STALE_STATE` with zero
commit/acceptance. A fresh revision was then used with the existing normal-save
path. Root's proof for the stale caller attempt is
`da47a3e8ec5f4687998ad4b8f9d9f4b3:1`, SHA-256
`A6E201060B3FDBD499CDC1FD3604DF0C4D35D632E7ADBBF5B6D313E3A11D8D4F`.

The successful normal save is recorded in
`8cec0b31a20746ba92a2fd61004c5419:1–13`: save action `4def…` completed at
tick `96611787`, with `R29/J100`. At this point external accepted is `10` and
lifetime is `170`; accepted actions are terminal, with no unknown or in-flight
write. Natural production after the save is not part of the saved state.

The subsequent read-only full-factory acquisition is
`80dd28e14adb4206b13c69166abd6805:1–78`: 60 entity pages, 5947 built
entities, zero prebuilds, 11620 reciprocal edges, 11 detail reads, factory
snapshot tick `96612508`, plus a separate prebuild observation at `96613323`.
Root's ten-write audit is `826763382f2241a0872ec0ec5222f393:1`, SHA-256
`F0506BC538CEBC4C49DADF9463CBDFCDA43D5D884648D94C2D2BBD0F0E085CC1`;
the focused diff is `5400616df9044f5387555bb12eb71ad3:1`, SHA-256
`548539DA38FC7389A9A0DC371E9BB29E3E525FB2CCCC6D511C9F3F75CF41DDC0`.
The audit found only new entities `5946/5947`, no removed entities and no
non-reciprocal edges; 216 dynamic changes are retained. Static differences
outside the exact `resourceNodeIds` subset are zero.

The only static subset losses are entity `1496` (iron ore `1001`, references
`47/46`) and entity `2440` (copper ore `1002`, reference `172`). The complete
active-node directory `57807d030c8a492e93ee3b59866ac09a:1–6` reports 97 active
`1001` nodes at tick `96636294` and 68 active `1002` nodes at `96636306`; all
retained references are present and the omitted IDs are absent. This is
compatible with natural depletion, not proof of exact depletion time, actor or
`remainingAmount=0`. No broader ignore rule was used.

The ten-write ledger has nine successful terminals plus one earlier failed
Move; all outcomes are resolved, with no replay, unknown or in-flight write.
The normal save at `96611787` covers the successful accepted work through
`R29/J100`; later natural production is not saved progress. The latest
independent observation is `96636320/R29`. No third tower or oil extractor was
built, and no oil outlet, source-to-consumer join, sustained supply or full
1210 chain was demonstrated. No deploy, close, restart or load occurred; DSP
and Steam remained running. The user-cancelled Host-exit survival test was not
performed. The whole plan remains `executable=false`; the next Game
write/commit remains frozen pending this evidence commit, its matching green
CI and explicit root handoff.

#### Rated legacy fuel model (conditional only)

Native base-model proof `0e43f65496974696ae7a441c1c97fe6e:1`, SHA-256
`4D2E1CF824F7589C64899054747322293DC05D4E2B48AB69849A1C9A9FF9BB69`, gives
energy values `H=9 MJ`, `G=6.75 MJ`, fuel rod `=600 MJ`. The three existing
hydrogen thermal generators have a rated total of `54 H/min`; the four fusion
generators, using the model's `6 rods/min` conversion, add a rated-equivalent
`120 H/min`, so the conditional legacy-hydrogen total is `174 H/min`, not
merely `120`. Six graphite thermal generators are rated at `144 G/min`. These
are model/nameplate
rates, not measured burn, proliferation, loaded-heat consumption, fuel stock
or stable demand. A point `P/C=0/0` therefore cannot be used to claim zero
legacy consumption or to close net-H/graphite allocation.

#### 2026-10-06 one-time source-qualification exception consumed

The previously authorized narrow exception is complete: the third `2201`
tower (`5948`) and the `2307` oil extractor (`5949`) were built from the
approved backpack materials, alongside the already recorded towers `5946`
and `5947`. The root-verified original window `f164f182824f49cfb8205c05e8822c04:1–58`
contains the two new terminal outcomes and exact material reconciliation;
there was no replay. The normal-save-only run
`2898f09523924f379fe548944b301da1:1–10` records save tick `96684808`,
`R34/J100`; root's independent save audit is
`e0a2b45e45664e6c9c0d45a80d01e8eb:1`, SHA-256
`D846C2313C0232E854DC671EBC47DE7FF5FA20D7DDB103BDC632B2FD5E840517`. The
current external window is `3`, lifetime `173`, with no unknown, in-flight or
unsaved accepted action. This consumes the one-time permission; it does not
authorize further construction, bulk material collection, handcrafting or
changes to existing lines.

The read-only source closure
`cb9b819e48fe48abb4746e41a8e8753f:1–9` observed source node `544` with
remaining amount `71473`, extractor `5949` with `50` units of output item
`1007` in its buffer, and N3 ratio `1`. A separate single 600-tick point
showed item `1007` production/consumption `2/4`; neither that point nor the
buffer establishes a continuous rate. The quoted three-source theoretical
`201.8887/min` is not measured output from the new extractor or proof of
allocatable supply. The N3 summary's dynamic capacity changed from
`1914000` to `1878000`; capacity is not a static topology identity.

The native outlet qualification `844161e156264fb2981e60f20c7a32fd:1–9`
returned a positive `native_device_port` prepare for a six-segment belt path,
with zero commit. Those belts were not built, and this did not prove an
actual-ID join to existing refinery `3964` or delivered oil. An unavailable
`sorterEndpoints` summary field is not evidence that the device lacks a belt
port. A local summary/capacity classification issue was corrected without
replaying any game action.

Whole-chain `executable=false` remains. The target's required net-H/graphite
and acid rates, legacy-fuel allocation, exact shared material/power budget,
and sustained source-to-consumer flow are not closed; gross r58 hydrogen,
theoretical source rates, point buffers and a single N3 ratio do not close
them. No deployment, close, restart or load occurred; DSP and Steam remain
running, and the user-cancelled Host-exit test was not performed. No additional
construction or long-window permission follows from this completed exception.

#### Same-stage material and power cut; route preparation pending

The completed point cut `a258aed5e4124496b2c62049e53fd878:1–10` contains 47
material objects and two complete cargo paths observed at the same tick
`96728601`; its closing observation is `96728635/R34`, while the latest normal
save remains `96684808/J100`. Root's independent proof is
`66c4d82ee6d547e0b73fa134ae2389ea:1`, SHA-256
`A4562812E367532643817232C8005A5C8DD7159D59FD557157794B98323D1FC4`.
Observed values include iron `1511=3000`, backpack iron `37`, gear `1202=1`,
belts `28`, sorters `7`; refinery `3964` oil `0`, `3965` graphite output `20`,
`3073` H/D `8/0`, `5326` D/strange-matter output `3/0`, `5333` lens `0`,
`5329` warper `0`, historical `5331` stock `2380`, and extractor `5949` oil
buffer `50`. These are point stocks/counters, not allocation or sustained-flow
proof.

The associated single 600-tick point reports P/C: H `7/2`, graphite `2/1`,
crude oil `2/2`; D, matter, lens, warper and fuel rods each `0/0`. It cannot
establish stable production or legacy demand. Nine thermal generators and
four fusion generators are on N3; the sampled capacity was `1914000 J/t`,
ratio `1`. A conditional headroom calculation is `1914000 - 1804700 - 14000
- 600 = 94700 J/t`, using the historical base, the installed oil extractor
and two proposed sorters. This is not measured burn or sustained power. The
native generator buffer fields are generation in J/t, not fuel stock or
consumption. The independent native outlet geometry/status verification
`5354c9f738c94454a2f7f83c96245f1a:1`, SHA-256
`82D433C3653391905DB047BE419F6AD4CF2E892120C29E14D55D69C1C011C8D9`, does
not contain or prove the 14 private offline power-checker fixtures. Those
fixtures are a separate offline result without a public evidence index; neither
is product/live evidence.

The three-source theoretical ceiling `201.8887/min` is not the new extractor's
measured rate. Existing target requirements remain net H `20/min`, graphite
`10/min` and acid `2/min`; gross r58 hydrogen must not be counted as net, and
legacy-fuel allocation is still open. The rated model earlier in this file is
conditional, not a measured demand or fuel inventory. Current source, power
and full-chain budgets remain pending synthesis.

Caller/summary errors were not native game failures: one request stopped
before dispatch because its entity-ID count was `60` against declared `61`;
another material-cut request included a generator, which the reader marked
unsupported rather than empty inventory. The corrected same-tick 47-object
cut completed. A final local PowerShell summary expression then failed after
the ten original responses were preserved; they were not rerun. No native
action was rejected or replayed because of these local projection errors.

Three ground-route spans in `1e064befdd4248288c21dca644fd325e:1–11` were
independently prepare-qualified at plan costs `21/21/18` (60 total) with zero
commit; the source-to-outlet preview in `844161e156264fb2981e60f20c7a32fd:1–9`
cost 6. The later elevated/tail records are
`f5a98d2af1bb4b9a8d01cfd38e91d915:1–9` and
`617539832c724e719d13ba4a51dda82b:1–9`: elevated spans cost `21/17/7`, and
the tail-only preview cost 5, for `116` across separate previews. This is not
a unique-entity count or a
joined exact bill: the tail and down-route share their first future anchor.
The tail's tokenless preview returned native pass/`Ok` for existing
`3964.slot1`, filter `1007`; the future joins still require fresh actual-ID
qualification. No route belt/sorter was built, and the forecast ceiling of
164 belts is not a material reservation. The source port preview remains a
separate prepare-only result.

The bounded tail read allowance was 8 requests; the executor used 9 because
of one additional read-only player observation. There were zero writes or
accepted actions, but the task is `readCapCompliant=false`; this overrun is not
silently waived. The new ground/elevated/tail prepare set had no native
geometry rejection and no successful prepare was replayed. A wrapper-layer
`sorterEndpoints` projection issue and a local summary-expression failure did
not change native outcomes. Root's independent combined proof is
`88b9edf932234725a859d4ef358211e4:1`, SHA-256
`C7E04D9ABD2322A0F9B84752C1F07DF531DAF049599A3D286AE186F7672DB226`;
latest close is `96766055/R34`, save `96684808/J100`, external `3`, lifetime
`173`. A separate offline parser check once failed on an unquoted 32-hex key;
that checker input was corrected offline and caused no Game call. It is not a
runtime failure.

For budgeting, one r16 plus two r58 cycles imply theoretical net H `45/min`
and graphite `30/min`, contingent on an actual graphite sink. An r58 cycle's
gross H output `3` against input `2` is net `1`; gross circulation is not net
supply. Existing legacy nameplate figures `174 H/min` and `144 G/min` remain
rated values, not observed burn, inventory or stable demand. The exact shared
net-H/graphite/legacy-fuel allocation remains open; do not infer a solution
from the 47-object point cut or native generator J/t fields.

The direct embedded-guide MCP test rebuilt the resource and passed `3/3`; the
offline package-surface policy passed `37/37`. The first test attempt had one
text assertion mismatch because Markdown backticks were not included; the
assertion was corrected to match the embedded text and rerun successfully.
These are offline documentation/policy checks, not product live evidence.
The one-time source exception is consumed; after full-chain qualification,
future execution remains governed by existing authorization and root's
bounded handoff, without inventing a per-action user reconfirmation gate.

## 2026-10-06 / shared-budget correction and native fuel observation (source only)

The revised complete source spans and five free join gaps completed in
`a890cadd4da3429d995e0b05dde9982f:1–25`: exactly25 reads,0 commits,5.6s native
execution. Root audit `ced9aeaaa5764329bc25fa051fd0f65a:1`, SHA-256
`78847CE9FC8AAE82059074977F7FAF3E8AA632124D77999A8A3302D181B56696`,
verifies spans20/20/17/20/16, gaps5/5/5/4/4, inventory conservation and
unchanged ownership/R34/J100/save96684808. Each future dual-cover join must
retain at least2 NEW points. The prospective full route123 is a partition
forecast, not an actual-ID joined bill. Actual cover joins still require
fresh native checks after their anchors exist. The historical tail9/8 read
cap violation remains recorded; this newer25/25 task does not erase it.

Root fixed-inlet trace `2f1d1485678646748c83e441f40e071f:1`, SHA-256
`C42A3C0C3A5FC174993D8ADA6451B7D2A72E2D1F119661483AFDCBCA6A263AAC`,
uses the original60-page5947-object snapshot at96612508 with reciprocal
edges and bounded explicit targets. It proves141/707 feed the old H thermal
branch,3073 receives3964 and four cracker outlets, and three odd-numbered G
thermal units receive only3083/3084 on their G branches. Inspection of the
same raw sorter connections also shows3060–3063 have both H and G inputs.
A current fuel ID therefore cannot prove an exclusive fuel allocation.
Null filters retain uncertainty; static reachability is not flow or burn.

The fresh budget prefix `25bd4f5f56044577b62a381fac341393:1–6` stopped on an
unavailable cut: its first9 selected objects were valid, then researchLab256
was unsupported. This was a root selection mistake, not damaged world state
or a rejected game action. Power summary at:5 succeeded; an initial writer
summary incorrectly implied it had not been reached. The remaining-only
suffix `2657a7ef42e348a29a65f0ff8148d487:1–8` removed onlyLab256 and read all23
supported objects at96878020. It completed in2.377s,0writes/0accepted; no
accepted action was retried. Root independently verified all14 original
receipts, caps, identities, Journal, stock units and exact native recipes in
`48d5f8297f4f429daa089c50b2cd21ea:1`, SHA-256
`5D33CA7B6B60327C81C19CD8CFB868D0F6AD43C54BB984D81FF0AD56C75AAE0E`.
Closing96878035/R34/J100/save96684808/external3/lifetime173 is unchanged in
write history; no in-flight, unknown or unsaved accepted action is known.

The fresh native catalog proves r5 makes gear1201, not magnetic-coil1202.
The backpack has iron37,coil1202×1,belt28,sorter7 and no gear1201. The former
gear1/material forecast was wrong: a164-belt ceiling needs46 r84 batches
(138 belts) and46 r5 gear batches,138 iron total,101 additional iron. A123
prospective count needs32 of each batch,96 iron,59 additional iron. These are
formula forecasts, not fresh handcraft plans or procurement authority. The
5187 coal-G source's4/min target is correct for Diamond; another6/min G for
Graphene comes from the cracker branch, giving10/min globally, not10/min
from5187. R103 uses G1 per ring, and r41 alloy1+D20+ring1 produces2 rods.

The separate600-tick native window96877428–96878027 records raw counts P/C:
crude2/2,G3/2,refined0/1,H6/3,D0/0,warper0/0,rod0/1.3964 was working;3073
H7 and5326 D4 were below full batches;5331 had2393 warpers. These are point
stocks and a short window, not new-oil attribution or continuous acceptance.
N3 power at the prefix's own tick has capacity1878000J/t,required/served
141843,ratio1. Its conditionally forecast full-base reserve1819300 leaves
58700J/t, not proof of sustainable fuel. Window item counts register native
item events, not continuous heat burn; loaded fuel can keep generating after
the buffered stack is empty.

This identifies the smallest required code repair: existing
`inspect_factory_entity` adds detail-only `fuelPowerState` for ordinary
2204/2211 generators. Current native assembly SHA-256 is
`6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`;
fresh decompilation verifies public field types and native fuel semantics.
Only identity-checked scalars are copied on the Unity main thread, including
separate buffered item count/inc/base heat and residual loaded heat/current
fuel/productivity state. Buffered and loaded fuel identities may differ.
No `EnergyCap_Fuel`, `GenEnergyByFuel`, `SetNewFuel` or other mutator is called.
Invalid/unsupported observations report unavailable with null quantities,
never fabricated zero. Generation/capacity retain J/t and full-width values.
No item injection, new tool, action/hash change, material-cut whitelist
expansion, automatic selector or planner is introduced; lists omit it.

Offline Release validation:52 relatedCore tests and5 MCP tests passed; full
solution including the native-DLL Plugin build has0 warnings/0 errors.
Source metadata probe reports64 tools/1 resource,exact embedded guide match
and0 extra stdout characters; package-surface policy37/37 passed. Native
fuel observation is **not installed or live-validated**, and no new ZIP,
game shutdown/restart, continuous credit or whole-chain executability is
claimed. Steam/DSP remain running. Existing3 dirty research/test files are
preserved outside this repair. Net H/G/legacy-fuel allocation remains open;
the source qualification exception stays consumed.
