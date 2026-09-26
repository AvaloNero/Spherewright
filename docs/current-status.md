# Current status

> **Historical saved-world baseline:** 2026-09-24. The separate September 26 re-entry section records newer checks; neither snapshot is a continuous assertion about the live game.

## Verified snapshot

- Documentation/evidence baseline: `346087b6b07c1b65b2e5567f5d24c141f4c9b739` (`2026-09-24T20:46:31+08:00`).
- Last recorded installed cohort: `f4fc8e5`; its native DSP version was `0.10.35.29088`.
- The owned-primary migration normally saved at tick `73573821`, retained Journal `J91`, and moved the accepted-action counter from `5` to `6`.
- The exact `0.10.34.28529 → 0.10.35.29088` reauthorization is one narrow local-live result. It retained the first 91 Journal entries and recorded one durable version transition. It is not a general version-compatibility, later-restart, factory-state, or production-continuity result.

## Current re-entry check (2026-09-26)

The game was closed at re-entry. The protected ticket still names the same saved checkpoint (`73573821 / J91`, accepted `6`) but has expired. The installed Plugin remains `f4fc8e5` and the native DLL remains `29088`. One Steam launch reached the main menu. A new preview-only expired-primary reauthorization passed exact identity/version/tick checks, with only `USER_CONFIRMATION_REQUIRED` remaining. The disclosure has been presented; no new load, save or construction occurred. See the [re-entry record](evidence/2026-09-26/reentry-preview.md).

After subsequent explicit user confirmation, one fresh same-primary recovery completed normally: save `73573852`, observed tick `73573875`, revision `1`, accepted `7`, owned/healthy and a newly issued restart credential. Independent verification proved matching runtime/handoff tickets and preservation of all first 91 Journal entries and the single historical version transition. See the [recovery acceptance](evidence/2026-09-26/same-primary-recovery.md).

The [fresh read-only factory snapshot](evidence/2026-09-26/factory-reentry-audit.md) established 5155 entities over 52 pages, zero prebuilds, healthy/J91 and accepted `7`. The later D.5/D.6 output-sorter pair and one normal save are now closed at `R6 / save73728691 / J91 / accepted10`; the one-time closing collection and static comparison are recorded in the [output-sorter audit](evidence/2026-09-26/hps-output-save-audit.md). This is a dated evidence snapshot, not a sustained-output, power/fuel, restart, or general factory-health claim.

## Completed work not to repeat

- Do not replay the old reauthorization token, normal save, or the already accepted `5 → 6` action. A later expired-ticket recovery is a new protected plan with its own disclosure and confirmation, not a replay.
- The original six-object blueprint module completed construction, partial cancellation, restart reconciliation, external connections and sustained output (approximately `29.989` graphite/min). Governor's separate predeclared `31 → 62/min` experiment passed `37128` effective ticks within its ±10% band. These historical bounded gates stay closed; they do not prove today's factory health or whole-version readiness. See the September 10 acceptance entries in the [diary](gameplay-timeline.md).
- Do not replay the completed west-side D.1–D.6 bounded construction stages or the normal save. The accepted counter is frozen at `10`; a later explicit root seal is required before any reset.
- Do not restart the historical `b849587` baseline plan, including its 48-belt / 8-attachment construction idea, merely because it appears in an old attachment or diary section.

## Still pending

- A later restart/resume check, all further construction, throughput, sustained-power/fuel, production, and final v0.4 acceptance gates remain outside the verified D.5/D.6/save closure.
- Formal release work remains distinct from the installed cohort and this local evidence snapshot.
- This status consolidation does not independently close the narrower declaration-restore/original-hash proof, 2012 sorter removal, nonzero-inc cargo preservation, post-repair resume or full composed transport-budget gates. Check the dated per-feature evidence before marking them complete; the blueprint/Governor throughput examples alone cannot substitute for those proofs.
- Cross-computer recovery and final-package validation are separate from all local-live results cited here.

## Which entry point answers which question?

| Need | Use | Boundary |
| --- | --- | --- |
| Latest verified source/evidence snapshot | commit `346087b6b07c1b65b2e5567f5d24c141f4c9b739` and the linked diary | Last verified, not a statement of today’s live state. |
| Last recorded installed code | cohort `f4fc8e5`, native `0.10.35.29088` | Installed evidence from 2026-09-24, not a formal release. |
| Formal release capability | released `v0.3.3` notes and the roadmap | v0.4 remains unreleased. |
| Exact raw and independent evidence | [gameplay timeline](gameplay-timeline.md), [IFX-171](incident-fix-log.md#ifx-171--过期-primary-重新授权冷部署受原生版本门阻断), and [EXP-304](experience-ledger.md) | Preserve their chronological and scope boundaries. |

## Re-entry rule

Start from the current recovery gate and fresh audit, not from a historical construction plan. Any later game action needs its own current authorization, fresh state, and bounded evidence; this page grants none.

## Engineering review follow-through

- [Ordinary belt input proof](evidence/2026-09-26/belt-input-proof.md): code and offline tests complete, not cold-deployed.
- [Installer integrity preflight](evidence/2026-09-26/install-preflight.md): 20 local synthetic cases pass; transactional replacement/rollback and final-package validation remain open.
- [Installer stage-only isolation](evidence/2026-09-26/install-release-stage-only.md): 30 local synthetic cases pass, including separate Plugin/MCP copy failures without live-target or recovery-state changes. Staging is not installation; promotion, rollback and crash recovery remain open.
- [Harvest approach watchdog](evidence/2026-09-26/harvest-approach.md): implementation, review and 2234 offline Release tests pass; not cold-deployed or live-validated.
- [Bounded normal-action history](evidence/2026-09-26/bounded-action-history.md): capacity/retention and active-only frame traversal implemented and independently reviewed; 2241 offline Release tests pass. Uncertain, recovery, failed-construction and checkpoint-flight evidence stays protected. Not cold-deployed or live performance-tested. Neither code change blocks the already approved sorter-only batch; no game capability is expanded to work around them.
