# Manual package CLI installation preview

## Input and method

This uses the existing dirty local manual archive
`artifacts/local-preview-20260927-bab392c/Spherewright-0.4.0-win-x64.zip`
(SHA-256 `0e422e2c3610f995c24d97004ae73b708593189e964038b4c0dce04b2732fc97`,
source commit `bab392c8a60e98db0b52f23475d4a9cfa59bd6d1`, `sourceDirty=true`).
`test-install-package.ps1` verifies its sidecar and manifest, extracts it to a
short GUID-owned temp tree, and invokes the extracted real `install.ps1` via
the current shell's `-File` path with explicit synthetic `-DspDir` and
`-McpDestination`.

The only DSP-shaped inputs are inert temporary `DSPGAME.exe`,
`Assembly-CSharp.dll`, and `BepInEx.dll` sentinel files. DSP is never started.
The MCP executable is the real packaged executable and its normal isolated
metadata probe runs during each successful install; no metadata stub is used.

## Results

The final package test passed in both shells:

| Shell | Session | Result |
| --- | --- | --- |
| Windows PowerShell | `45190` | exit `0`; four stages passed |
| pwsh | `97094` | exit `0`; four stages passed |

Each run proved, inside its independent temp tree:

1. `-PreflightOnly` verified the package without creating or changing a live
   target, stage parent, archive parent, or marker.
2. A first install with both Plugin and MCP live payload roots absent committed;
   exact package payload hashes, paired committed progress records, and marker
   removal were read back.
3. A same-version reinstall without `-Force` rejected and left the full
   synthetic target region unchanged.
4. After the test wrote a protected `runtime-handoff` sentinel, `-Force`
   committed a same-version reinstall while retaining that sentinel verbatim,
   exact payload hashes, paired committed records, and no marker.

The first Windows PowerShell wrapper attempt is superseded: redirected
`Start-Process` exposed a null `ExitCode`, and `[int]$null` incorrectly became
`0`. The test wrapper now binds its owned child process handle before waiting,
rejects a null raw exit code, and polls stdout/stderr against a 1 MiB rejection
threshold with a 120 s timeout that may terminate only that child. This is not
an OS-enforced hard output cap. Independent owned `exit 0` and
`exit 7` smoke children returned their true Int32 codes in both shells.

## Boundary

This is a real-payload CLI test, not a real DSP, Unity, native-version,
clean-machine, or user-directory installation. It does not start the game,
load/save a world, deploy, publish, tag, or prove Thunderstore runtime behavior.
The preview's manual `INSTALL.md` still has its separate old-version boundary.
