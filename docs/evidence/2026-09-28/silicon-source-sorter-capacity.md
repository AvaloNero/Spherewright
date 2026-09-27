# Silicon-source sorter capacity read

Local live read-only evidence, 2026-09-28. Source `35901dc`, installed
cohort `034300e`, native DSP `0.10.35.29088`. This is a bounded source-side
diagnostic, not a construction or throughput-acceptance result.

## Bound read boundary

After the saved same-star arrival boundary (`R32 / save75920947 / J91`), Terra
made 21 real Bridge read requests and zero writes under
`action-dafed114c5b74a3c8e72c5df91b0406d-`. The first local descriptor binding
attempt failed before any Bridge request; it changed no game state. The
audited wrapper retained two protected response copies per real request, so
the prefix contains declaration `0001`, response copies `0002`–`0043`, and
completion `0044`. The collection did not query Journal; `J91` here is the
already-confirmed arrival boundary, not a fresh Journal read.

The opening and closing sessions are the same owned, healthy local planet102
session at `R32 / save75920947`, with no blocker or flight checkpoint. The
player was grounded (`Walk`, speed0), core energy `800000000 / 800000000`, and
had normal graphite fuel storage `77`; no movement, flight, save, prepare or
commit was issued by this collection.

| Native window | Start–end tick | 1003 produced / consumed | Observed rate | Scope checks |
| --- | --- | --- | --- | --- |
| 1 | `76014020–76014619` | `5 / 0` | `30 / 0` per min | ready, 600 ticks, complete 3/3 factories, no cross-session |
| 2 | `76014818–76015417` | `5 / 0` | `30 / 0` per min | ready, 600 ticks, complete 3/3 factories, no cross-session; starts after window 1 |

Both native rows report one direct diagnosed producer, no item1003 finding,
theoretical `132/min`, and utilization `22.727…%`. These are two independent
short windows, not a long-window or sustained-output certificate.

## Current path and capacity boundary

The already-reviewed static route includes the existing input sorter26
(`belt24 → warehouse25`) before the selected single source path
`25 → 174 → 179 → 178 → 44`. Sorter26 comes from the prior arrival static
snapshot `f159e351c6d84aaebe92514f7cb83cde`, not from this batch's eight
detail targets. All eight selected details were read once after each window.
Warehouse25 holds thirty 100-item silicon buffers (`3000`) in both reads.
Sorter179 is filtered to1003 and its observed local buffer changes `0 → 1`;
the observed buffers for belt148, belt176 and terminal belt178 are empty in
both detail snapshots. Station44's silicon slot rises `254 → 261` of500, with
equal local/remote supply counts and demand `246 → 239`; its station input
remains belt178. Its resident/working vessel fields remain zero in both reads.
These inventory and buffer snapshots are taken beside, not at the exact tick
of, the native windows, so they are not a conservation proof or an exact
delivery rate.

The existing native transport formula gives input sorter26 (`STT400000`,
span2) a rated maximum of `45/min`, sorter174 (`STT600000`, span3) `30/min`,
and sorter179 (`STT400000`, span2) `45/min`. The present serial route is thus
capped at `30/min` by174 under the full-power, single-item, no-backpressure
model. Upgrading only the two downstream interfaces would give their combined
path `min(60,90)=60/min`, but unchanged input26 would keep the complete
steady path at no more than `45/min`. Even all three 26/174/179 interfaces as
ordinary2012 would rate only `60/min`, below the two furnaces' `120/min`
declared demand. The focused `FoundryTransportPlannerTests` filter passes
`26/26`; that verifies the current formula, not world throughput. This is a
constrained-path calculation, not an authorization to upgrade, build, alter
station logic, add miners, or infer dispatch failure. The next step remains a
separately approved fresh plan for the complete serial path; no parallel
candidate has passed.

## Evidence and limits

Completion proof:
`action-dafed114c5b74a3c8e72c5df91b0406d-0044-planet102-silicon-two-window-read-complete.json`,
SHA-256 `78A4FADF6B629CF4C11E20D9CE06B852CFBA2FC097AD56EDCE61412545025688`.
It preserves both window DTOs, the eight exact entity readbacks per window,
player energy, and the matching closing session.

This does not prove a sustained source rate, station dispatch, end-to-end HPS
delivery, remote silicon repair, new miner need, power/fuel headroom, a return
flight, or a later restart.
