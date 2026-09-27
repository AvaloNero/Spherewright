# Path-budget package preview

## Input and package result

This is a dirty local preview from source commit
`3d154231fde48701707d172cc47b2fd771286c0f` (`sourceDirty=true`), written to
the new `artifacts/local-preview-20260927-path-budget-3d15423/` directory.
It did not overwrite an earlier preview or change any held source work.

`package-release.ps1 -Version 0.4.0 -AllowDirty` completed its Release build
with 0 warnings and 0 errors. It reported these verified artifacts:

| Artifact | SHA-256 | Result |
| --- | --- | --- |
| Manual `Spherewright-0.4.0-win-x64.zip` | `9af68b83c5bb80157df958c89f099416d498ae338e61313951b8d98c215a0c71` | 239 file entries (241 ZIP entries including two directories); sidecar matches |
| Thunderstore `Spherewright-0.4.0-thunderstore.zip` | `5beea155c52c42c1014bf264ecc38239bb85a354eb0e738af4d7e1f42df5cf79` | 12 entries; static structure verified |

The manual archive has one `Spherewright-0.4.0` top-level directory. Its
manifest reports package/product version `0.4.0`, source commit `3d15423…`,
`supportedDspVersion=0.10.35.29088`, and 238 manifest file records.

## Packaged path-budget surface

Readback from the actual manual ZIP found `install.ps1`,
`SpherewrightInstallTransaction.ps1`, and `SpherewrightInstallRecovery.ps1`;
all three contain `Assert-SpherewrightInstallPathBudget`. The same archive
contains `RECOVERY.md` (4,929 bytes) and `AGENT-PLAYBOOK.md` (89,566 bytes),
so the recovery guidance and playbook are included with this preview.

The existing manual validator passed with protocol `2025-06-18`, server
`Spherewright.Mcp` version `0.4.0.0`, 64 tools, one resource, and matching
packaged/embedded playbook content. The existing Thunderstore validator passed
the expected version, source commit, checksum, icon, and static layout checks.

## Five-stage manual-install regression

`test-install-package.ps1` passed against the actual manual ZIP in both shells:

| Shell | Result |
| --- | --- |
| Windows PowerShell (session `56774`) | exit 0; five stages passed |
| pwsh (session `12233`) | exit 0; five stages passed |

Each owned temporary tree proved a zero-write preflight, first-install commit,
unchanged same-version rejection without `-Force`, forced reinstall with the
test-owned `runtime-handoff` sentinel preserved, and a partial-root first
install recovery. The final case uses the archived stage, the existing internal
transaction interruption hook after main-plugin promotion, and the archived
`recover-install.ps1` preview/restore CLI; it is not a claim that ordinary
`install.ps1` itself crashed. Successful installs used the real packaged MCP
metadata probe. The test reported `gameLaunches=0`.

## Boundary

This is an offline package and synthetic DSP-shaped-target validation only: no
real installation, game process, save, deployment, tag, or publication changed.
It does not prove arbitrary long real target paths, a clean machine, Unity/DSP
runtime behavior, cold deployment, or a general power-loss/legacy-startup-race
recovery guarantee. Earlier preview ZIPs remain unchanged; this evidence applies
only to the dirty `3d15423` preview above.
