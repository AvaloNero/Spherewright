# Fixed autosave recovery inspection

## Authorization and scope

The user explicitly authorized these bounded read-only checks on an ongoing
basis. AGENTS records that authorization in `19a2765`. No save-directory
enumeration, arbitrary named-save access, native load, overwrite, ticket edit,
or construction replay was performed. DSP was closed.

Current native `GameSave` static-initializer/AutoSave IL inspection identified
four rotating slots (`AutoSave0` through `AutoSave3`) and `AutoSaveErrored`.
The temporary `AutoSaveTmp` file was excluded. The existing verified game
installation locator and native document-path override logic resolved their
directory without executing GameSave/GameConfig static initializers.

Root performed the actual candidate reads at `2026-09-27T11:33:27Z`, after
taking over a delayed directory-resolution preparation. Earlier preparation
updates must not be read as completed file inspection. The existing
`OwnedSaveRecoveryLease.Open` reads the bounded identity prefix first and
rejects mismatches before hashing file content; its 64 MiB/2-second budget and
read-only sharing were unchanged. Each matching file was opened twice, with
the same evidence fingerprint returned on both opens.

## Results

| Native slot | Game tick | Native version | Same protected identity | Stable | Covers last observed tick 75203871 |
| --- | ---: | --- | --- | --- | --- |
| AutoSave0 | 75214675 | 0.10.35.29088 | yes | yes | yes |
| AutoSave1 | 75185882 | 0.10.35.29088 | yes | yes | no |
| AutoSave2 | 75157094 | 0.10.35.29088 | yes | yes | no |
| AutoSave3 | 75128308 | 0.10.35.29088 | yes | yes | no |
| AutoSaveErrored | 6062594 | 0.10.34.28529 | yes | yes | no |

All prefixes reported peaceful mode. AutoSave0 is later than both successful
sorter5158 at tick75194266 and the last observed tick75203871. Its composite
lease fingerprint is
`sha256:37baa6a79fa87963243de737b29b5542fa2b5ee393fe0bb6b599124c13bf787a`.
That is not a standalone save-file SHA or proof that entity5158 is intact.

Runtime and handoff ticket hashes agree, but the ticket has now expired and
its minimum tick remains73728691. It was not renewed or consumed. The exact
ticket-bound Journal file has91 contiguous entries, matching journal ID,
owned identity hash, tracking mode, coverage flag, start tick and minimum91.
Its origin version remains28529 with one valid durable transition to29088
at73573790/J91; no historical version or entry was rewritten. Journal SHA-256:
`10A7982B959626A5D82FDF0386E7DA7FF96DBF2F3BA50292247B8318FEDCC334`.

## Evidence and remaining boundary

Root command receipts: `6218cf` (five slots), `67c89f` (Journal sequences),
`7be539` (checkpoint fields), `2bca93` (identity hash/version transition).
These are read-only inspection receipts, not game action IDs.

The earlier conclusion that neither primary nor LastExit covers construction
remains true; the new evidence adds a sufficiently recent same-identity fixed
autosave candidate. The remaining boundary is a supported, fresh, explicit
recovery path for that candidate with expired-ticket handling, followed by
post-load entity/material/connection and Journal reconciliation. Inspection
authorization alone does not authorize loading it. No entity persistence,
resumed production, save/resume gate or whole-v0.4 completion is claimed.
