# Fixed AutoSave0 recovery: source implementation

## Scope

The user authorized this narrow loader addition after the
[read-only candidate inspection](fixed-autosave-inspection.md). It extends the
existing prepare/commit tools, not the tool count or an arbitrary save picker.
`reauthorize_expired_autosave0` is explicit and resolves only the native
`GameSave.AutoSave0` field verified against the current29088 DLL. There is no
slot/name/path input, fallback or copying/renaming of saves.

An expired, healthy, unconsumed ticket with matching protected replicas and its
original exact Journal checkpoint supplies provenance, not permission to load.
Prepare requires a known progress floor and exact expected candidate tick,
forbids confirmation, and returns dedicated evidence-version3 disclosure and a
plan digest. The candidate must match the full owned identity, peaceful state,
source/current native version and expected tick, cover the known floor, and be
strictly newer than the ticket. This mode does not perform version migration.

## Primary overwrite target and commit

Independent review identified an additional overwrite-target risk: an
unreadable or different-identity primary cannot be treated as an unknown tick
that permits later normal overwrite. The new mode therefore also requires the
original primary to exist, prove the same identity/version/peaceful state and
exact ticket tick. Both full-file lease fingerprints enter the prepared digest.
Missing, corrupt, locked or changed primary evidence rejects without loading
or consuming credentials. Existing resume modes retain their own rules.

Commit requires a subsequent explicit user reply and matching digest. It
revalidates menu revision, ticket/Journal provenance and both save files, holds
their read-only leases, records the attempt and durably consumes the expired
credential before calling the native loader. Source selection is an exhaustive
internal enum switch; unsupported values cannot silently select the primary.
The original-primary lease is held with the candidate and Journal leases until
adoption and Journal confirmation. Normal owned-primary save and a matching new
credential are required for successful terminal status. The existing post-load
identity/planet/peaceful checks and candidate..candidate+3600 tick window remain.

This does not claim cross-process atomic protection between lease release and
normal saving. The ordinary save/header-readback checks remain necessary.
Interruption after durable consumption requires reconciliation, not replay.

## Validation boundary

The linked-production coordinator and actual ticket store run against test-only
native-loader/session/ACL substitutes. Sixteen synthetic cases pass, covering
disclosure without load, subsequent confirmation, consumption before the fixed
loader, idempotent replay without a second load, candidate/Journal/revision and
primary drift, missing-slot no-fallback, native failure after consumption,
original-primary refusal cases and write-lock retention through loader entry.
They do not prove Unity loading, real session adoption, real Windows ACL setup,
normal game saving or entity5158 persistence. An initial fixture error generated
different timestamps for the two synthetic ticket replicas; the test was fixed
to use identical timestamps, without weakening product replica checks.

The complete Release solution build passed with zero warnings/errors. The
Core.slnf Release suite passed 2313 tests (59 Contracts, 190 MCP, 2064 Core).
Source MCP metadata verification passed: version0.4.0.0, 64 tools/1 resource,
exact packaged-playbook text match and zero extra stdout. Independent static
review found no remaining blocker. No deployment or actual load has occurred
in this source slice. The user must still confirm the later fresh runtime
disclosure before the first actual recovery commit.
