# Fixed AutoSave0 recovery: local native result

## Authorization and execution

After the [native disclosure](fixed-autosave-cold-deployment.md#subsequent-native-main-menu-preview),
the user explicitly replied to recover. Luna used the installed034300e cohort
on native `0.10.35.29088`, made a fresh main-menu read and a new version3 prepare
for the same fixed candidate75214675 and known progress floor75203871, then
committed once with subsequent confirmation and the fresh digest/idempotency key.
The expired earlier preview token was not reused. No other source was loaded,
no copy/import was created, and no construction was replayed.

Action `f4cafd04-3fe0-4a62-8c5a-90bfae13215c` was accepted once, not an
idempotent replay. Polling that same action returned `completed`, terminal and
succeeded, with no recovery required. The action result itself has null tick
fields; the save and observation ticks below come from explicit later reads.

## Initial post-load evidence

- Normal owned-primary save tick75214706 covers the loaded candidate75214675.
- Session read tick75226640/revision1: owned, healthy, writes allowed, peaceful,
  non-sandbox, saved state and a new available protected-resume capability.
- Journal91/91 is durable, with no pending persistence or error. Its existing
  `attached_existing_save`/incomplete-history coverage semantics remain; this
  recovery does not fabricate earlier history.
- Player position and inventory match the last recorded post-build player
  snapshot. Item2012 count is2, Walk/speed0, core800MJ, three idle drones and no
  queued handcraft or pending build target.
- Sorter5158 remains item2012/filter1003, with pick1976/insert1980. The source
  and destination slot5 edges reciprocate sorter slots1/0. Both older slot4
  edges to1981 remain. No new build or extra material deduction occurred in
  this recovery packet.
- The external accepted-action window advances1→2 for this one recovery. Its
  internal normal save is not counted as another external commit.

The initial caller summary mistakenly reported2012 quantity1 by confusing the
single inventory entry/slot count with item quantity. Root checked the original
JSON against the old player receipt and corrected it to2; this was not missing
inventory or a game-side defect. Evidence must use the item's explicit count,
not the size of the filtered collection.

## Evidence boundary

Protected raw prefixes: `542769c018754f9abe94925abbdd8c98` (fresh reads,
prepare and unique commit), `9809615d1ef741ba9ff8e45fa8c4642b` (terminal),
`a99c029b70104c51943a767689a96c24` (session/player/Journal/three entity reads).
Root independently read the prepare, terminal, session and old/new player
receipts. Terminal evidence-file SHA-256:
`8AB4AF459F85AC155E967BD8E920653ECDB42BA214749BC8292B5317F7EE17E6`.

## Independent credential/history review and factory comparison

Sol independently verified matching new runtime/handoff credentials, same owned
identity, new token, restored session and normal-save tick75214706; the original
attempt and consumption tombstones match across both protected locations. The
old and new Journal snapshots are identical after excluding only sessionId and
capture tick: all91 entries, original/current versions, the existing version
transition and tracking/coverage fields remain. The exact Journal file SHA-256
still matches the pre-recovery evidence.

Root collected the current planet104 factory once: 54 read-only calls in
15.513 seconds, 52 built pages belonging to one snapshot at75258800, 5158 unique
entities and a separate empty prebuild result. The previously delegated caller
had failed at PowerShell parsing before any live call; root took over using the
existing audited client. That preparation delay is not native load/collection
time or a failed game action.

Offline comparison reused `Assert-AuditConfig` against the previously sealed
5157-entity baseline. The sole new entity is5158. All5157 old entities retain
their checked static identity, pose, configuration and connections after
removing only the declared new5158 neighbor edges from1976/1980. All10054 current
directed connections are reciprocal, exactly four more than the baseline.
Dynamic cargo, buffers, progress and power readings are not cross-tick equality
fields. This covers the listed local factory, not a new survey of other planets.

| Protected proof | SHA-256 |
| --- | --- |
| `aa0554f30507446b9831560a67f44b13` / collection0055 | `A95C1FAEF068D8DCF824145E5D242554D881092B00C98EBA1665A8FF33935C03` |
| `019937d731924756ac4cc725d610976d` / comparison0001 | `65536A69E7635645D31DB69AE8A3F0B226CE3B566A1132B4F9993EFD2C5FE6C2` |

This result closes the fixed-candidate restore and5158 persistence check, not
sustained HPS or purple-matrix production, the two unsubmitted attachments,
another restart, cross-computer recovery or whole-v0.4 readiness.
