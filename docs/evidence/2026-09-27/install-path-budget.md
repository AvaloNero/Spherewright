# Installation and recovery path budget

## Defect and change

The [real-package interruption test](install-package-interruption-recovery.md)
found Windows PowerShell failures in deep recovery evidence and cleanup paths.
Shortening its test layout established a bounded positive example, but did not
make production installation paths safe.

The new shared read-only check budgets the live payload, stage and archive
layouts, original backups, displaced payloads, progress records, pending marker,
per-attempt recovery/follow-up locations and MCP metadata-isolation directories.
Payload-relative paths come from validated expected-file maps; fixed-width
operation and attempt placeholders match the real 32-character identifiers.
This is deterministic path calculation, not a filesystem scan or relocation.

The cross-shell compatibility policy is at most 259 UTF-16 code units per file
path and 247 per directory path, including parent directories. These are
conservative product limits, not universal filesystem limits. Microsoft's
[path-length guidance](https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation)
explains the legacy bounds and why OS support alone does not imply every
application has opted into long paths. No registry setting or extended-path
namespace is changed.

Checks run before a successful install preflight, before actual staging,
before transaction records/marker/backups are created, and in recovery context
validation after mirrored expected-file maps agree but before deeper payload
inspection or recovery writes. Recovery commit re-reads that context. An unsafe
old interrupted installation must preserve its marker and evidence for review;
this change does not move it to a shorter path or edit recorded identities.

`StageOnly` keeps its existing direct-parent requirements. Ordinary installation
and its preflight calculate the actual nearest existing staging ancestor when
the final MCP parent is absent, then recheck the chosen path before staging.

## Verification boundary

Both supported shells completed the same source-script suites with exit code 0:

| Suite | Windows PowerShell session | PowerShell 7 session | Result per shell |
| --- | --- | --- | --- |
| Transaction | 94947 | 3599 | 13 groups, 82 mutation-fault injections |
| Recovery | 83611 | 6263 | 3 groups, 2 test-child interruptions |
| Installation preflight | 13526 | 72467 | 56 cases, 0 game calls |

The added cases cover 259/260 file and
247/248 directory boundaries, nested MCP paths, the longer archive/per-attempt
paths, and direct transaction rejection with unchanged fixture tree, live main,
marker absence and no transaction records. These are calculated boundary and
synthetic filesystem tests, not actual files created at every length limit.

Recovery tests also prove that a legacy interrupted record with an over-budget
expected map is rejected without changing its fixture tree, main DLL or marker.
Preflight tests invoke the source installer with `&` in the current shell; they
are not fresh archived-package `-File` tests. They preserve rejection of
`StageOnly` when its direct MCP parent is absent. Independent Sol review found
no blocker; the reviewer did not duplicate the test runs.

No real installation, DSP process or save was changed. Earlier preview ZIPs
remain immutable and do not gain this source-only check; a new package and
cold deployment require separate evidence.
