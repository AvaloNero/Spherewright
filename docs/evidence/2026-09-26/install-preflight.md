# Release installer integrity preflight

The old installer checked a manifest subset, copied Plugin files before confirming the MCP payload was usable, and overlaid targets without ruling out extra files. A later rejection could leave a mismatched installation.

The new bounded slice validates all source manifest paths, uniqueness, sizes and SHA-256 values; rejects unlisted files/reparse points; requires exactly four approved Plugin assemblies and the MCP executable; checks all three source/target path pairs for overlap; and validates both existing destination trees before the first write. `-Force` no longer admits unapproved leftover payloads. Protected Plugin `runtime-handoff` data is retained, never copied/deleted; the external runtime is not an installation target. A successful copy is followed by destination exact-set/hash verification.

`-PreflightOnly` performs these checks without installing. It reports `installed=false` and `transactionalUpgrade=false` explicitly.

Evidence is **offline synthetic only: 20/20 cases passed**, with parser and whitespace checks and independent Sol review. Tests cover malformed/missing/extra payloads, all source/Plugin overlap directions, MCP overlap, reparse points, target leftovers, no-write preview/rejections, an exact successful copy, and preservation of synthetic runtime/handoff state. The script never discovers or modifies a real game installation. Public Core CI does not run this new standalone PowerShell suite yet; the 20-case result is a local run, not a CI claim.

This is **not transactional upgrading**: copy-time I/O failure, crashes, concurrent filesystem changes, cross-directory switching and rollback remain unresolved. No real release package, cold deployment or final v0.4 acceptance is claimed. Existing held packaging/CI changes are excluded from this slice.
