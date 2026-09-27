# Real-package partial-root installation recovery

## Scope

This extends the existing [manual-package CLI test](install-package-cli.md), using
the same unchanged dirty preview archive, not a newly built final package:
`Spherewright-0.4.0-win-x64.zip`, SHA-256
`0e422e2c3610f995c24d97004ae73b708593189e964038b4c0dce04b2732fc97`.
Its source commit is `bab392c8a60e98db0b52f23475d4a9cfa59bd6d1` and
`sourceDirty=true`.

Only GUID-owned temporary trees are used. The DSP-shaped inputs are inert
sentinels; no game process, real installation or save is touched. The original
Plugin root contains only a test-owned runtime-handoff sentinel, and the MCP
payload root does not exist. This is a **partial-root first install**, not a
cross-version upgrade.

The actual archived installer runs `-StageOnly`, including the real packaged
MCP metadata probe. An owned child then calls the unmodified archived transaction
function with its existing internal test hook, interrupting after the main DLL
has been promoted and before final verification/commit. The archived recovery
CLI provides a read-only preview and an evidence hash; a subsequent explicit
restore must return to the original partial root and absent MCP payload.

This is not a crash of the ordinary installer CLI itself. It also does not
establish arbitrary interruption coverage, power-loss durability, Unity startup,
latest-source cold deployment, Thunderstore runtime behavior or game recovery.

## Results

The first run used a nested `partial-root-recovery` fixture directory:

- PowerShell 7 session `51903` passed all five stages (exit0).
- Windows PowerShell session `3647` failed in the archived recovery CLI:
  `Move` reported that part of the path could not be found, and the follow-up
  attempt to withhold the live main DLL reported the same error.

The test cleaned its owned temporary tree in `finally`; the failure output does
not include the actual source/destination pair. Given the known temp-root
length, fixed directory components and 32-character operation/attempt IDs, the
main-DLL failure-evidence destination computes to261 characters. This is strong
evidence of a Windows PowerShell/.NET Framework path-length problem, not an
independent readback of that removed destination. The recovery catch location
is not the originating `Move` call.

The source uses `transaction/r-<attempt>/p/unresolved-live-main.dll`, and a
similar `f` path for follow-up withholding. It currently does not preflight the
length of future recovery paths before installing. Short-path success must not
be promoted to arbitrary Windows installation-path compatibility. A write-free
preflight rejection or separately verified long-path support remains a concrete
installer safety gap.

The first controlled rerun shortened only the fixture subdirectory to `r`, with
the package and restoration logic unchanged. PowerShell7 session84071 passed
all five stages and exited0. Windows PowerShell session96579 emitted the
five-stage success result, but its final cleanup failed while removing a deep
MCP recovery-archive file, so that entire test exited1 and is **not** a pass.
Its one known residual test root was subsequently removed with PowerShell7
after checking its exact GUID basename and temp-parent containment; no other
temporary root or real installation was selected for cleanup.

The final short-fixture run also uses the shorter `swip-<32-character-GUID>`
test-root prefix, retaining strict temp-descendant and exact GUID-basename
cleanup checks. **Windows PowerShell session41803 and PowerShell7 session75982
both passed all five stages and exited0**, including final test-tree cleanup.
Neither shorter fixture changes the ZIP, product recovery logic or the
unresolved production long-path limitation.

The new fifth stage verifies the staged MCP handshake and exact payloads,
the hook's flushed sentinel and expected stderr, both preterminal records at
`copy-plugin-main-last`, the promoted main-DLL hash and pending marker,
zero target/stage/archive writes during preview, then exact restoration of
the handoff-only Plugin tree, missing MCP root, removed marker and paired
`rolled_back` archive records. The first four stages also reran after the
test consolidated its owned-child process runner. This closes the named
short-path real-package partial-root recovery example, not the installer P1
as a whole. Next: reject unsupported future recovery paths before live writes.
