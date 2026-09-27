# Recover an interrupted manual installation

This entry point restores the **captured original payloads**, not the newest
release. It does not open or repair game saves, choose an installation for you,
or prove that the captured old installation was healthy. Keep DSP and the MCP
host stopped. It is separate from protected game-save resume.

Preserve the pending marker, both operation directories, their progress records
and backups. Do not remove the marker to bypass the startup refusal. Use the
explicit operation ID and Plugin/MCP destinations from the failed installation;
do not choose a directory merely because its timestamp is newest.

The manual package's `recover-install.ps1` defaults to a read-only preview:

```powershell
./recover-install.ps1 -PluginDestination '<BepInEx/plugins/Spherewright>' `
  -McpDestination '<the exact MCP installation directory>' `
  -OperationId '<the failed installation operation ID>'
```

Replace the placeholders with the exact failed operation's local values. If
preview passes, inspect its operation ID and `evidenceHash`. Explicit restoration
requires that hash:

```powershell
./recover-install.ps1 -PluginDestination '<BepInEx/plugins/Spherewright>' `
  -McpDestination '<the exact MCP installation directory>' `
  -OperationId '<the same failed operation ID>' `
  -RestoreOriginal -ExpectedEvidenceHash '<the fresh preview evidence hash>'
```

Each call acquires both target locks and checks the exact marker-bound stage or
archive on each side. It requires matching immutable records, complete original
backups, recognized live file hashes, unchanged `runtime-handoff`, and no reparse
points or unexpected payload files. Different progress phases can be a normal
consequence of interruption between sequential record writes; different
identities or original snapshots are not. The preview hash binds both actual
record files and the current evidence. A changed hash requires a new preview,
not forced reuse of the earlier command.

Restoration first withholds any live main Plugin DLL, including an older binary
without a startup guard. It restores other Plugin files and MCP, verifies those
original payloads, then restores the main DLL last. It only removes its own
pending marker after both terminal records and the final original payloads have
been verified. Previously absent files remain absent. Displaced files and old
backups remain as evidence; newly created empty ancestor directories may remain.

A failed restoration preserves the marker/evidence and attempts to withhold the
main DLL again. If that withholding also fails, the error reports it. Do not
blindly replay a previous restore command; inspect the failure and obtain fresh
evidence. The tool does not automatically retry, scan other backups, repair
malformed records or overwrite files with unknown hashes. Truncated records,
incomplete backups or unexplained file changes require separate investigation.

## Compatibility and validation boundary

Only the manual package's supported target layout is covered, not arbitrary Mod
Manager layouts. Explicit recovery may acquire an abandoned installation mutex
and then revalidate everything; ordinary installation and Plugin startup still
refuse an abandoned lock. Recovery is an explicit operation, not an automatic
startup behavior.

The new Plugin's pending-marker gate does not exist in old binaries. Moving an
old main DLL out of the scan tree cannot stop one that was already loaded.
There is no claim of atomic cross-volume upgrades, protection from all hardware
failures, or universal recovery of partially written evidence. Local synthetic
tests, source builds, cold deployment and real-package validation are separate
evidence levels; consult the repository's `docs/current-status.md` before
treating an implementation as live-validated.

The installer and recovery preview budget the complete future paths, including
stage/archive roots, backups and per-attempt recovery evidence, before writes.
For compatibility across supported PowerShell hosts they conservatively require
file paths of at most 259 UTF-16 code units and directory paths of at most 247.
These are product compatibility limits, not a claim that all Windows APIs share
the same limit; see [Microsoft's path-length documentation](https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation).
Shortening only the live destination may not be sufficient: a path-budget rejection identifies
the generated path that exceeds the budget. For a new installation, choose a
shorter supported target and rerun preflight. For an already interrupted
installation, keep the exact targets, marker and evidence unchanged and seek
review; do not move its evidence tree or edit recorded paths to bypass refusal.
The tool does not enable system long-path settings or rewrite paths to extended
namespace syntax. Compact evidence names alone are not proof of safe recovery.
