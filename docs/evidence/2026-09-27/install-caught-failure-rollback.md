# Installer caught-failure rollback

## Scope

Default manual installation now uses the same verified double staging and isolated MCP metadata checks as `-StageOnly`, followed by a local two-payload installation transaction. The two target paths are locked; both original payloads are copied to same-volume backups and verified before live replacement. Exact old/new/backup hashes, preserved handoff state and the closed-game condition are rechecked before promotion. Plugin's main DLL is kept out of the recursive loading tree while the remaining payload changes; MCP uses a same-volume directory swap.

The final installed MCP executable must pass the isolated metadata probe, and both live payloads must match their manifests. Caught failures attempt restoration of the old file sets, including previously absent files. The Plugin directory and `runtime-handoff` remain in place: protected contents are fingerprinted, not copied, renamed or deleted. Failed/new payload evidence and old backups are retained in operation-specific directories rather than discarded.

Both payload sides retain progress records flushed to disk. Nonterminal, missing, malformed or inconsistent paired records block another installation; a single terminal mirror is insufficient. First installation stages under an existing ancestor, records missing parent directories before creating them and checks higher-ancestor residue on a later attempt. Newly created empty parents may remain recorded after failure; this is not a whole-filesystem rollback claim.

## Validation

- `test-install-release-preflight.ps1`: **43 checks passed separately under Windows PowerShell and PowerShell 7.6.5**. The suite covers the existing preflight/staging gates, default install/reinstall, first-install missing parents, initial and post-promotion metadata failure, unchanged protected state and both directions of deliberately split terminal/nonterminal archive records. The opposite target is changed in each split-record retry, so the terminal side must actually inspect its counterpart. Metadata here is a stub, not a real executable handshake.
- `test-install-transaction.ps1`: **7 scenario groups passed separately under both shells**, including **76 injected failures before enumerated forward steps** across upgrade and first-install paths. This covers normal success/reinstall, recorded parent creation, callback failure with verified old payload restoration, a rollback dependency failure with the main Plugin absent and `needs_recovery`, and a real exclusive file lock on the second journal mirror. The latter proves that a persistent journal failure does not suppress old-payload restoration, and leaves unresolved evidence rather than success.
- The real source-built MCP probe remains separately covered by the [metadata suite](staged-mcp-metadata.md). This slice does not turn the installation fixture's stub into a real package handshake.
- Independent Sol review checked the live-before-write gates, reverse replacement order, journal failure handling and paired archive checks. Test cleanup resolves and bounds its unique temporary directory before recursive removal.

All fixtures are temporary synthetic installation trees; no real DSP installation, game load, save or construction is part of these checks. The 76 injections are not an exhaustive storage-hardware or process-kill fault model.

## Explicit limitations

`caughtFailureRollbackSupported=true` does not mean that every I/O failure is recoverable. If restoration or durable recovery-state recording fails, evidence is retained and the operation requires recovery; it must not report installed success or permit an automatic retry. A journal failure does not suppress the attempt to restore old payloads.

This is not an atomic cross-volume upgrade, power-loss/process-kill recovery, startup recovery gate or final release-package validation. `crashRecoverySupported=false` and `transactionalUpgrade=false` remain explicit. No tag, release or Thunderstore publication is authorized by this slice.
