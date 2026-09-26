# Native-version dual-package preview

## Scope

This dirty local preview changes both generated package source manifests and
the two current Thunderstore supported-scope lines from `0.10.34.28529` to
`0.10.35.29088`. Historical v0.3.3 validation text was not changed.

The existing entry point used `-Version 0.4.0`, `-AllowDirty`, the local .NET SDK,
and the previously absent `artifacts/local-preview-20260927-bab392c` output
directory without `-Force`. The archive records commit `bab392c8a60e98db0b52f23475d4a9cfa59bd6d1`
and `sourceDirty=true`; it is not a clean release candidate.

## Actual package results

The entry point completed locked restore, full `Spherewright.sln` Release build
(**0 warnings, 0 errors**), regular self-contained MCP publish, and Thunderstore
single-file MCP publish. Packaging does not rerun the C# test suite; the earlier
2254-test result belongs to the separate `386d3f4` evidence.

| Archive | SHA-256 | Existing validation result |
| --- | --- | --- |
| `artifacts/local-preview-20260927-bab392c/Spherewright-0.4.0-win-x64.zip` | `0e422e2c3610f995c24d97004ae73b708593189e964038b4c0dce04b2732fc97` | The existing manual-package validator passed: manifest/file hashes, package surface, 64 tools, 1 resource, metadata handshake exit `0`. |
| `artifacts/local-preview-20260927-bab392c/Spherewright-0.4.0-thunderstore.zip` | `56798e06473ff37de83f0bb9b2c44e1456d251ca8aa5ef8e5617288a4e713f68` | The existing Thunderstore validator passed static structure and checksum checks. `runtimeBlackBoxTested=false`. |

Read-only archive inspection after both validators confirmed:

- Both manifests declare `supportedDspVersion: 0.10.35.29088`, the same source commit, and `sourceDirty: true`.
- The Thunderstore README contains both current Chinese and English target-version lines; its manifest retains `xiaoye97-BepInEx-5.4.17`.
- The manual archive contains `recover-install.ps1`, `SpherewrightInstallRecovery.ps1`, `SpherewrightInstallTransaction.ps1`, and `RECOVERY.md`; all four hashes match its manifest.

## Known boundary

The archived manual `INSTALL.md` still declares only `0.10.34.28529`; this is
an unresolved preview boundary, not a final install-ready package. A package
`supportedDspVersion` is a distribution target, not the source or authorization
for an owned-save migration; the bounded owned `0.10.34.28529 -> 0.10.35.29088`
evidence remains separate.

No real installation, Unity/DSP start, game call, load, save, construction,
deployment, tag, release, or Thunderstore publication occurred. This does not
prove clean-environment installation, runtime black-box behavior, owned-save
migration, or v0.4.0 release readiness.
