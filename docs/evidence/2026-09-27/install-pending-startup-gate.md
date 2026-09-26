# Pending installation startup gate

## Scope and change

Source baseline: `7fbe7fff62f166da64b60133ae8301559a62dd4b`.
This follow-up adds a durable fixed pending marker and a startup lease shared by
the manual installer and the new Plugin. It is not an offline crash-recovery
writer, a new MCP capability, or a live installation.

The installer creates the marker exclusively and flushes it before any planned
live-parent creation or live payload replacement. Existing markers block even
preflight/stage-only attempts. Marker removal requires this exact operation's
identity, verified live payloads/handoff and matching paired terminal archives.
A deletion or verification failure is unresolved, not installed success.

The new Plugin acquires the same target mutex before configuration/host
creation, refuses any marker or inability to establish absence, and holds the
lease until the Bridge host is disposed. Abandoned mutex ownership is released
correctly on both refusal paths. See the [runtime API and compatibility
boundary](../../research/installation-startup-guard.md).

When a caught rollback or final marker cleanup remains unresolved, the
installer attempts to move any live main Plugin DLL outside the recursive
loading tree, retaining payload and backup evidence. Old dependencies and MCP
may have been restored while the main DLL is intentionally withheld: that is
not a complete rollback and must remain `needs_recovery`. If withholding fails,
the error must say so rather than promise the old Plugin cannot start.

## Validation

- Complete Release solution build: zero warnings/errors against current local
  game references. The full build caught a DSP global `Mutex` name collision
  absent from linked Core tests; the guard now explicitly aliases
  `System.Threading.Mutex`.
- Contracts/Core/MCP Release tests: **2254 passed** (`59 + 2011 + 184`), including
  **12 startup-guard tests**. Two use actual Windows PowerShell and PowerShell 7
  child processes to verify lock contention and release. This is Windows/.NET
  evidence, not Unity Mono live evidence.
- Installation integration: **55 checks passed separately in Windows
  PowerShell and PowerShell 7.6.5**. The 12 added checks prove zero-write refusal
  for empty/malformed/terminal-looking markers or a directory at the marker
  path, across preview, staging and default installation. MCP metadata in these
  synthetic fixtures is a stub, not a real package handshake.
- Transaction tests: **12 scenario groups with 82 forward-step fault injections
  passed separately in both shells**. They cover successful marker cleanup,
  prior-marker refusal before transaction creation, first-install marker-create
  failure without live-parent creation, post-commit marker-delete failure with
  new dependencies/MCP retained and new main withheld, and rollback-marker-delete
  failure with old dependencies/MCP restored and old main withheld. The existing
  exclusive journal-file failure now requires unresolved main withholding, not
  a false complete-rollback claim. A deletion-hook identity replacement remains
  untouched, with `needs_recovery`, exact promoted dependencies/MCP and the main
  DLL withheld in evidence. These are caught synthetic faults, not actual
  process-kill or storage-hardware failure tests.
- Independent Sol review checked the startup/installer lock pairing, cleanup
  ordering, marker identity checks and the new-versus-old unresolved payload
  assertions. No game writer participated in this installation slice.

## Not proved

No game launch, load, save, deployment or construction occurred in this slice.
The last recorded installed cohort remains `f4fc8e5`, distinct from source.
The unsaved sorter recovery boundary remains unchanged: primary `73728691`
predates successful terminal `75194266`, Journal `J91`, external accepted `1`.

Old Plugin binaries do not honor the new lease or marker. Withholding on caught
failure cannot remove an already loaded old host or close every legacy startup
race. Process-kill/power-loss restoration, durable-directory metadata semantics,
explicit offline recovery, actual Unity startup refusal, cold deployment,
final same-commit packages and clean-environment install/upgrade remain open.
`crashRecoverySupported=false` and `transactionalUpgrade=false` remain accurate.
The full installer P1 and v0.4 release gate are not closed. No tag or publication.
