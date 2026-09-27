# Remaining HPS infeeds and normal save

## Narrow completed packet

After the fixed-AutoSave0 recovery had restored the owned, saved boundary at
`75214706 / R1 / J91`, Luna ran the already-bounded continuation once.  Its
protected prefix is `action-fed0e2468db947b3af624ab41ee96a60-`; it contains
two accepted, non-replayed `2012` builds and one accepted, non-replayed normal
save.  The caller's completion proof is `0072`, with `accepted=5` and no
further writes requested.

| Terminal receipt | Verified result | SHA-256 |
| --- | --- | --- |
| `0020` | Build `5159`, `2018 → 2020`, tick `75382560`, item2012 `2 → 1` | `F3102B42C0EC52F71D608DD8FDAAE48FE689F43784CF50548519BAAE86A97DF8` |
| `0050` | Build `5160`, `2024 → 5116`, tick `75384015`, item2012 `1 → 0` | `97F2B87B560FDFD18608EB1B49A4B1D6E41C02C3F25648A6FCF0C511DDFB5B5C` |
| `0068` | Normal save, tick `75384857` | `3F9F9A216381BAF4CDA8AF51CAC88198C58526B16C969584FAB39A7F95F11515` |
| `0069` | Final owned session is `R6`, saved at `75384857` | `103AA8D52F6BE5354E1662D418BE6CC27A9B7544F03BDB0022DF797AF6CE21B4` |
| `0072` | Caller completion proof | `4DAB14CBF6808378BA49474E1402082E237EABF07CD240C17C4ADC09E5FE040D` |

Both new entities are item2012/filter1003 inserters on power network3 with
`powerServeRatio=1`.  Receipt `0021` proves `5159.slot1 ↔ 2018.slot5` and
`5159.slot0 ↔ 2020.slot5`; `0051` proves `5160.slot1 ↔ 2024.slot4` and
`5160.slot0 ↔ 5116.slot4`.  The matching belt receipts prove each reciprocal
edge.  The caller compares endpoint connection keys after excluding only the
new sorter edge, so it rejects any other endpoint-connection change.

The final player read retains item2012 count0 with no pending build or repair
target.  The final Journal still has 91 durable entries and reports neither
pending persistence nor an error.  This packet therefore proves these two
attachments and the ordinary save, not an ongoing supply rate, HPS output,
power/fuel headroom, or a restart after this batch.

Sorter5158 is not freshly inspected by this batch.  Its recovered persistence
and reciprocal `1976/1980` connections remain the separately scoped result in
[the fixed-AutoSave0 recovery evidence](fixed-autosave-recovery-live.md).

## Next boundary

The next step under the existing continuation authorization is a bounded
**read-only** chain window for actual input, power, and output observations. These
construction and save receipts do not substitute for that observation or
authorize another action.
