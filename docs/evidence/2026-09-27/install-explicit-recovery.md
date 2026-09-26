# Explicit offline installation recovery

## Scope and change

This slice adds a deliberately explicit, target-bound offline recovery path
for an installation that already has this operation's pending marker and
paired transaction evidence. It is not automatic recovery: the caller first
requests a read-only preview, then supplies that exact preview's evidence hash
to request restoration of the original payloads.

The recovery path uses the explicit manual `BepInEx/plugins/Spherewright`
target and the explicit MCP target. It derives only the matching marker,
stage/archive roots and paired progress records for the supplied operation ID;
it does not scan arbitrary installation trees. It verifies old-payload
backups, known old/new live content and the protected `runtime-handoff`
fingerprint before permitting a restore. The evidence hash is recomputed under
the two target locks immediately before mutation. A stale or mismatched hash
therefore performs no recovery write.

An explicit restore withholds any currently live main Plugin DLL, restores the
original payload sets, records paired `rolled_back` archives and removes the
marker only after those checks pass. A recovery failure preserves the marker
and evidence; its live main DLL is withheld rather than reported as a complete
rollback. This is an offline/manual path, not a new MCP or game capability.

## Validation

- `test-install-recovery.ps1`: **3 groups and 2 real child interruptions**
  passed separately in Windows PowerShell and pwsh. Each child is created by
  the test and terminates itself with `Environment.FailFast`; no name-based
  process termination is used.
  - Upgrade interruption: the child stops at
    `progress-verify-installed`, after the new main Plugin DLL is live.
  - First-install interruption: the child stops at
    `copy-plugin-main-last`, with both Plugin and MCP live roots absent before
    promotion. After explicit recovery, both roots again match that absent
    original state.
  - Both cases preview first, reject a mismatched evidence hash, then restore
    the exact original Plugin/MCP payload state and protected handoff, remove
    the marker, and retain paired `rolled_back` archive records.
- The interrupted upgrade also proves five read-only refusal paths before its
  approved restore: a missing old backup, an unknown live non-main payload,
  legal old/new live drift after preview with a stale evidence hash, a mirrored
  immutable-record mismatch, and protected handoff drift. Each refusal leaves
  the synthetic tree unchanged; the test restores its deliberate byte changes
  before the normal recovery call.
- One additional internal recovery-finalization fault is injected after the
  old main DLL has been restored. It leaves `needs_recovery` and the marker,
  restores old non-main dependencies/MCP payloads, and moves the old main DLL
  back outside the scan tree. A fresh preview then permits the subsequent
  explicit restore.
- The mechanically shared fixture did not change the prior transaction suite:
  `test-install-transaction.ps1` passed **12 scenario groups with 82 fault
  injections** separately in both shells. Root also reran
  `test-install-release-preflight.ps1`: **55 checks** passed in each shell.

All of these trees, payloads, process interruptions and metadata are
synthetic temporary fixtures. No real DSP installation, ZIP, Unity process,
game launch, load, save, construction or deployment occurred. There is no C#
change in this slice. The prior **2254 tests/full Release** result belongs to
`386d3f4` evidence and was not rerun or claimed here.

## Not proved

This bounded explicit recovery does not prove arbitrary process-kill or
power-loss recovery, cross-volume atomicity, storage-hardware failure safety,
real package installation, cold deployment, Unity startup behavior or a
clean-environment upgrade. It does not close the legacy startup race: an old
Plugin already loaded before the new guard cannot be retroactively governed.
`crashRecoverySupported=false` remains the correct public capability claim;
the completed result is a manually initiated, evidence-bound offline restore
in synthetic tests only.

Game state is unchanged by this engineering evidence. The primary remains
`save73728691`; persistence of sorter `5158` at terminal tick `75194266` remains unproved,
with Journal `J91` and external accepted count `1`. No tag, release or
publication is authorized.
