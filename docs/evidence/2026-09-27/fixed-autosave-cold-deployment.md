# Fixed AutoSave0 recovery: local preview and cold deployment

## Source and package scope

Source `034300e758c9a2339b4f9a913ad90b99b2e89046` was pushed to main;
Windows CI run36319503014 passed. Its [source tests](fixed-autosave-recovery-source.md)
are separate from the installation evidence below.

Both local v0.4.0 previews were built from that commit with `sourceDirty=true`
because six previously held packaging/CI files remain local. These are not
final release candidates. No tag, GitHub Release or registry publication occurred.

| Archive | SHA-256 |
| --- | --- |
| Manual win-x64 | `bc8e2de2bd8469b380fd54618fd9e13a537609e79695b918b651d9e38396e0f4` |
| Thunderstore | `975afe90f4f9bce49b1d23db5a8c685aa902fa42e6e83559ae1c36684e179439` |

The full Release build passed without warnings/errors. The manual validator
verified 238 manifest records, 64 MCP tools/1 resource and exact embedded
playbook match. Thunderstore static validation passed with12 archive entries;
this is not Mod Manager or Thunderstore runtime validation. Both Windows
PowerShell and PowerShell7 passed the existing five-stage manual-package
fixture checks, including explicit partial-root recovery. Those fixtures made
zero game launches and did not use the actual installation targets.

## Actual local installation

The first package installer preflight correctly refused three old development
PDBs outside the four-DLL release allowlist. Only those three explicitly named
Spherewright debug-symbol files were moved to a new local backup; all hashes
matched after the move. They were not deleted. DLLs, runtime-handoff and saves
were not moved, and the installer allowlist was not weakened.

The same package then passed real-target preflight. With DSP and MCP stopped
and both native compile-reference hashes matched, the package's normal installer
completed its paired Plugin/MCP transaction with status `committed` (operation
`d76bb2ef74674ea2982edadb479112df`). Exact runtime credential, original Journal
and handoff credential hashes were unchanged across installation. The installer
verified final payload integrity and isolated installed MCP metadata.

A separate single read-only manifest comparison matched all228 installed
payloads (four Plugin assemblies and224 MCP files), with no hash differences.
Plugin SHA-256 is `69FBB0B09253E2A5F057500CCC441DE95792D10A3C68543C79CBF33D9E45E515`,
with ProductVersion `0.4.0+034300e758c9a2339b4f9a913ad90b99b2e89046`.
The installed MCP executable SHA-256 is
`364297F50D3B115F6095DB84FDF45607A92D1BD6EB4C65EA7D3431972B6AA654`.

Protected evidence prefixes: `bc879f607f3d4287a517cf7fa8709586` (recoverable
symbol backup), `2b07f97c5d4c4e12bdb38b93f0fff824` (installation intent/result).

This stage proves local installation, not world loading, entity5158 persistence,
normal resave, resumed production or whole-v0.4 readiness. The next step is a
fresh runtime disclosure and subsequent user confirmation before any load.

## Subsequent native main-menu preview

Steam was not running. The three previously known registry installation values
normalized to one executable; one Steam `-applaunch1366540` request started DSP.
There was no direct game-executable launch or retry. Fresh Bridge/session reads
reported native `0.10.35.29088`, Plugin0.4.0, no loaded world, unowned/read-only
main menu and revision0. Unknown peaceful/sandbox values at the menu are not
loaded-world evidence.

One `prepare_resume_owned_game` for `reauthorize_expired_autosave0` succeeded,
with known floor75203871, exact candidate75214675, evidenceVersion3, verified
embedded identity and source/target29088. Its only blocker was
`USER_CONFIRMATION_REQUIRED`; `commitAllowedNow=false`. The dedicated disclosure
requires preserving the original identity/Journal, no fallback or imported
copy, durable expired-credential consumption before load, and normal saving
before a fresh credential. Interruption after consumption requires investigation,
not replay. No commit, native load or save was attempted. Later confirmation
must use a fresh plan with the same verified scope, not this preview token.

Root independently read the actual prepared reply recorded at
`2026-09-27T12:50:40Z`, evidence prefix `7a35ae8022394d339f6fd4ee2e874c42`.
Its redacted evidence-file SHA-256 is
`CD5BA0951E0C49355F4D640BEFC678C26F4F1FFCD6EC5FF6B30F76049AD821AB`.
This is the first positive native-main-menu preview for the new mode, not a
successful recovery. The redacted evidence file is not a reusable plan token.
