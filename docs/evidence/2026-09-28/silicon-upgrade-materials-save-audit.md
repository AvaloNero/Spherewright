# Silicon-route upgrade materials, return and saved audit

Local live evidence, 2026-09-28 (Asia/Singapore), `owned-world-001`.
Source/documentation parent: `1d7c0ce`; installed cohort remains `034300e`,
DSP `0.10.35.29088`. No product code or installed payload changed.

## Closed actions

The window began with the [verified flight and arrival save](silicon-source-flight-arrival.md)
(accepted 1–2). The following normal actions complete the same bounded window:

| Accepted | Action | Native start → completion tick | Evidence prefix |
| --- | --- | --- | --- |
| 3 | Move to a previously observed grounded point | 76071136 → 76074490 | `7d06ff7bcb1943e898c5650d0bb5d1c5` |
| 4 | Harvest copper ore ×1, node83 | 76113181 → 76113495 | `8cd18112ae11460a9f620941b03fe40c` |
| 5 | Short approach to iron14 | 76128567 → 76128876 | `5bc4d02283eb40eea7aa5d75f494474b` |
| 6 | Harvest iron ore ×5, node14 | 76141856 → 76142942 | `1e3917fe45b24e7bab71b1c10c4f4c84` |
| 7 | Recursive native recipe88 ×1 | 76154611 → 76155091 | `f55f137eb0c542b0adcd816d6a5da70c` |
| 8 | Return to the established grounded point | 76162896 → 76164128 | `c02cd66712b34b129f626640071986db` |
| 9 | Fresh short approach to the silicon interfaces | 76164157 → 76164387 | same prefix |
| 10 | Normal save | completed76164422 | same prefix |

All accepted actions reached successful terminal results without replay. The two
short moves had fresh observed ground previews with zero unknown/shore-risk
samples; historical long destinations were not presented as collision-free routes.
Recipe88 consumed iron ore5, copper ore1 and basic sorter2; it produced fast
sorter2 and one spare magnetic coil. Other backpack quantities/inc were preserved.

## One saved-world audit

Eight read-only requests under `588e2deaa597484fa0c4a54c2c7d65c6` captured two
pages in one factory snapshot: **179 entities, 320 reciprocal directed edges,
zero prebuilds**, with static configurations equal to the arrival baseline.
Closing tick76176576 is `R47 / save76164422 / healthy`; player is Walk0,
800MJ with an empty craft queue and the exact post-craft backpack.
All original91 Journal entries and version transitions equal the arrival record;
durableThroughSequence=91, persistencePending=false, no persistence error.
Power at the sample was capacity55000/required5625 J per tick, service ratio1;
this is not a full-load budget or sustained power acceptance.

Collection proof ordinal0009 SHA-256:
`E9B82CF3863EA13A864F02B709C720A18125B533A8CCEF4F82FF0784561ED5BE`.
Offline Journal/backpack proof `96db58a40e7f40658dd5de7820f7a9ad/0001`, SHA-256:
`BA2EE308FF811D80DA439489335DC0075F926EA932677E9F389DFE8F6AD010B9`.
The existing movement-watchdog/surface-preview filters passed **37/37 offline**;
this was not a full-suite rerun or a substitute for the native receipts.

No sorter upgrade, parallel interface, new miner, sustained silicon supply,
station dispatch or later restart has passed in this stage. Accepted10 remains
frozen until independent review, commit/push, green CI and explicit handoff.
Next: separately authorize one existing sorter174 upgrade, then fresh-test the
parallel source interface using its normally refunded basic sorter.
