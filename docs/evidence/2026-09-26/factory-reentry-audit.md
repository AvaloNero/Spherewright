# Fresh post-recovery factory snapshot

Installed cohort remains `f4fc8e5` on DSP `0.10.35.29088`. This stage made **zero game writes**; accepted remained `7`, revision `1`, last normal save `73573852`.

- Session at `73600845`: owned and healthy, no save error.
- Journal at `73600857`: all 91 entries durable, no pending/error, original `28529` origin and one historical transition to `29088` retained.
- Complete immutable entity snapshot at `73600869`: **5155 entities / 52 pages**. Complete prebuild snapshot at `73601566`: **0**.
- Inventory: two basic sorters (`2011`), three fast sorters (`2012`), one basic belt (`2001`). Both power networks were fully served at observation time; this is not sustained-fuel proof.
- Twenty-one selected entities were inspected at `73601568–73601622`. The west HPS furnace `5115` retains recipe `59`, its existing input sorter `5126` and staged output belt topology. Both new wind generators `5154/5155` belong to network 3.
- Independent Sol review found the two **unsubmitted** output attachments still valid as candidates: `5115.slot7 → 5137` and `5151 → 843.slot10`, both basic sorters filtered to `1105`. Those endpoint slots are free. Storage `843` retains its 30-grid/28-banned configuration and a remaining filtered `1105` slot; its observed stock was `1113 × 100`, not evidence of `1105` receipt.

Private raw prefix: `action-b27473ea40fb462d8fe2417e93d63469-`. This is a full entity-list snapshot plus 21 detailed inspections, **not** a claim that all 5155 objects' configurations/cargo were compared with their pre-restart state. Natural inventory and production may change between captures.

Next bounded intent: the two fresh fully prechecked attachments, then one normal save only if both succeed and settle, reaching accepted `10` and the audit freeze. No old D.1–D.4 replay, no additional feed/parallel sorter, and no continuous-HPS-output claim. Any rejection, unexpected identity/topology/material state or unknown terminal stops the batch without trying an alternate layout.
