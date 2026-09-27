# Existing silicon sorter174: native upgrade

Local live evidence, 2026-09-28 (Asia/Singapore), `owned-world-001`.
Documentation parent `473ba1b`; installed cohort remains `034300e` on DSP29088.
The previous ten-action window was independently audited, pushed and passed
CI36345474135 before this new external accepted window began at0.

One normal `2011 → 2012` upgrade completed at **tick76223336**. Source and
result IDs were both174 in this action; the caller did not assume ID stability.
Raw prefix `dc058fd9f53d437baa55fcc2dd9dbb52`: ordinal0007 is the fresh
allowed prepare,0009 the unique non-replayed commit,0010 the successful
terminal result without recovery or outcome-unknown reconciliation.

The native result preserves both connections, recipe/filter0 and held silicon
`1003 ×1 / inc0` in the same-action before/after buffers. The native cycle
policy is `basic_sorter_cycle_fraction_retained`, with required progress
`600000 → 300000`. Later fresh reads preserve `25 ↔ 174 ↔ 173` and the
neighbors' other edges. Backpack fast sorters decrease `2 → 1`, basic sorters
increase `0 → 1`, and all other item quantities/inc remain equal.

Final session is `R49 / accepted1 / healthy`, with the primary save still
**76164422**: this upgrade is **not yet normally saved or restart-verified**.
Network1 has capacity55000 and required=served6549 J/t, service ratio1 in
that sample. Instantaneous demand/held cargo are not frozen across later ticks.
This does not prove sustained power, complete-route throughput or delivery.

Protected completion proof ordinal0017 SHA-256:
`1D673448BD4FF48094B7D85B3A0059C013BE6713AEBE5C1BE9C0B84F24F16A75`.
Independent review checked the original prepare, commit, terminal, reciprocal
endpoints and material proof. `BuildingUpgradePolicyTests` passed **44/44
offline**; no product code changed or full-suite rerun is claimed.

Next: use the refunded basic sorter for a fresh native preview of the bounded
parallel `24 → 173` input. No parallel construction is proved by this upgrade.
