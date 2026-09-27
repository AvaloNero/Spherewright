# Silicon-source expedition kit: normal crafting and save

Observed 2026-09-28. Source baseline `c2331c9`; installed cohort `034300e`,
native DSP `0.10.35.29088`. This is local live evidence, not release-package,
cross-computer, flight, or remote-construction acceptance.

## Ten accepted actions

The previous fuel window was sealed only after its audit, push and green CI.
This new window ends at `accepted10 / R28 / save75836737 / J91`.

| Step | Normal action | Terminal tick | Result |
| --- | --- | --- | --- |
| 1 | Move to the previously observed Walk position | 75767916 | Walk, speed0, core800MJ; no inventory change. |
| 2 | Transfer iron1101 from warehouse723 | 75775774 | Storage2700→2643, player0→57. |
| 3 | Transfer copper1104 from warehouse723 | 75797695 | Storage100→93, player0→7. |
| 4 | Transfer magnet1102 from warehouse723 | 75801695 | Storage400→390, player0→10. |
| 5 | Recipe48 ×1 | 75834902 | Miner2301 +1; iron−8, magnet−2, copper−2. |
| 6 | Recipe8 ×2 | 75835145 | Tesla tower2201 +2; iron−4, magnet−2, copper−1. |
| 7 | Recipe7 ×2 | 75835691 | Wind turbine2203 +2; iron−14, magnet−6, copper−3. |
| 8 | Recipe84 ×9 | 75836537 | Belt2001 +27, total28; iron−27. |
| 9 | Recipe85 ×2 | 75836713 | Sorter2011 +2; iron−4, copper−1. |
| 10 | Normal save | 75836737 | Saved/healthy, revision28, restart capability available. |

Craft counts are native recipe executions, including ordinary recursive
dependencies and time. Their total consumption is exactly iron57, copper7,
magnet10. This is a portable spare-parts budget, not a claim that the remote
site needs those buildings. No building, flight or station configuration was
committed. All accepted actions completed successfully without replay.

The first copper commit (`c7e45d57365a4088bc904ea8b09393f1`) returned explicit
`STALE_STATE`, with no accepted action. A subsequent caller guard
(`9521cc2bb4724dff9954e874ed027490`) incorrectly kept starting revision12
after actual revision15 and stopped after a session read, before prepare.
The one fresh native copper retry succeeded; neither refusal counts as an
accepted write. Later copper94 or iron2700 readings reflect normal warehouse
production, not a reversal of same-action transfer conservation.

After the execution leaf spent over five minutes preparing without a craft
request, root verified no in-flight action and supplied a fixed six-action
suffix. Its no-Execute entry rejected before imports/game calls; Sol reviewed
it independently and Luna ran it once. This caller correction is not a Plugin
fix or a successful remote repair.

## Final audit and protected evidence

Terra made one 59-request read-only collection: 52 same-snapshot built pages,
one empty prebuild page, two session bounds, player, Journal, power and
warehouse723 detail. Against the preceding `9f600…` baseline, all5160 old
entities pass `Assert-AuditConfig` without removing any connections. All10062
directed edges remain reciprocal; no entities, edges or prebuilds were added.
Warehouse723 detail matches its page's static configuration. Inventory net
changes are only the five declared products, with unchanged inc; the forge
queue is empty. The original91 Journal entries remain equal and durable,
without pending persistence or error. Network3 service ratio is1 at the
snapshot. Graphite fuel storage200→195 reflects normal consumption, not an
inventory injection or a sustained-fuel-supply proof.

- Move/iron/copper/magnet receipts: prefixes `16ee8a00cd214f2a84c2fd2da5aea97f`, `5533d0318c764faeaa958ce746cd05ca`, `02f8ec8fd04a4fb388b35b0ab30904bc`, `ec148efd762740369fa82c140ce0dbd0`.
- Five crafts and save: prefix `91c5315e30a84af3b7f5f09cc46c27bf`; final save action `2e5f6ae7-4882-42c5-b766-d2bb2546bf39`.
- Collection: `f26fe6475c01403186830257a7e2308d/0060`, SHA-256 `07277C06B7FB18983184648A0247DB643CA7CEDE5ADC1A9DA470608D41E454AF`.
- Offline audit: `a6858a1eff50451cb820966b35e6afa0/0001`, SHA-256 `77D2EE8118945E6330920E9489C7EB571001633C332D0A4DA0F139F1762E105B`.

This does not close sustained silicon/HPS/purple supply, remote diagnosis or
repair, a subsequent restart, or the full v0.4 readiness gates. The next
bounded step is a fresh same-star planet102 flight preflight; no old token or
flight script is reusable authority.
