# Fuel staging and ten-write audit

Observed 2026-09-28; documentation source baseline `6268870`, installed
Plugin/MCP cohort `034300e`, native DSP `0.10.35.29088`. This is local live
evidence, not a final release-package or cross-computer validation.

## Frozen boundary

The already-authorized window is now frozen at `accepted=10`, `R12`, saved
tick `75650743`, and durable `J91`.  Terra then made one 67-request,
read-only final collection: 52 same-snapshot entity pages, one empty prebuild
page, nine affected details, player, Journal, power, and two session bounds.
It made no game writes.  Its protected collection proof is
`action-9f600b8a48f649ee8832875ec6e0052f-0068-fuel-staging-ten-final-read-collection.json`
(`C29DAD4A835A79B9A11921DF094C158435ABBB06E3503A9BAE6E551086DADA50`).

| Window step | Protected evidence | Narrowly verified result |
| --- | --- | --- |
| 1 | historical `cb0ddae…` build | Sorter5158 was built once for `1976 → 1980`. |
| 2 | fixed-AutoSave0 recovery `f4cafd04…` | The same owned world was recovered; 5158 was retained. |
| 3 | `fed0e246…`, build5159 | `2018 → 2020` at tick `75382560`. |
| 4 | `fed0e246…`, build5160 | `2024 → 5116` at tick `75384015`. |
| 5 | `fed0e246…`, normal save | Saved at `75384857 / R6`. |
| 6 | `e5dcb2e…`, Move | The separately bounded approach move completed; this audit does not prove flight. |
| 7 | `9cdee58…`, transfer `a855e50c…` | Item1109 transfer from source114: `3000 → 2800`, player `+200`, tick `75638926`. |
| 8 | `7c20ed…`, refuel `4daa1e56…` | Item1109 `100` loaded, player inventory `-100`, tick `75650693`. |
| 9 | `7c20ed…`, refuel `d4ae3943…` | Item1109 `100` loaded, player inventory `-100`, tick `75650712`. |
| 10 | `7c20ed…`, save `fc3b786d…` | Normal save terminal at `75650743 / R12`. |

The four new terminal receipts above are accepted, non-replayed, terminal,
successful, and not recovery-required. Their exact protected prefixes are
`action-9cdee58f40b34778963abde157c26486-` (transfer) and
`action-7c20ed304381407e9aaf2d82116a8bd7-` (refuels and save).

## Static factory result

The sealed baseline has `5157` entities and `10050` reciprocal directed
edges.  The independently recovered snapshot has `5158 / 10054`; the final
snapshot has `5160 / 10062` and zero prebuilds.  Offline comparison with the
existing `Assert-AuditConfig` helper passes all three deltas:

- sealed → recovery: only `5158`;
- recovery → final: only `5159` and `5160`;
- sealed → final: only `5158`, `5159`, and `5160`.

Every older entity's static configuration remains equal after removing only
the six approved belt attachments.  All final directed edges are reciprocal.
The final detail readbacks for `1976`, `1980`, `2018`, `2020`, `2024`, `5116`,
`5158`, `5159`, and `5160` match the factory snapshot.  The three chains are
`1976 → 5158 → 1980`, `2018 → 5159 → 2020`, and
`2024 → 5160 → 5116`.

The original player had item2012 count `3`; the final read has `0`.  The
final player read at tick `75665329` has item1109 inventory `0`, but one
occupied fuel-storage entry with item1109 count `200`; its reactor item ID and
reactor energy are both `0`.  This is the actual end readback, rather than an
assumption that transfer quantity equals native fuel consumption.

The transfer caller stopped after its successful terminal because it used a
`configurationStateHash` guard that also changes with buffer contents.  The
source transfer and static endpoint checks passed; this is the existing
IFX-135 caller-guard recurrence, not evidence of a Plugin rejection or a
factory configuration change.  The protected incident check is
`action-1a4e55b42eab44bca54744621defbf57-0001`
(`F3E0DF1F64DC6E53674C398B2F69D1E7049AB28C3D907F7E1F5ED3F3465027D8`).

The offline comparison record is
`action-0b1d212b763b43f4a72eec73c3172cf4-0001-fuel-staging-ten-offline-static-audit.json`
(`3439D5F28F9A9844B708136C87DB3B963FFC28D46CACC4589624327235D9562E`).

## Not proved

This closes only the declared construction, fuel-staging, and save boundary.
It does not prove sustained HPS throughput, production headroom, a remote
silicon-route repair, flight behavior, or a later restart/resume.
