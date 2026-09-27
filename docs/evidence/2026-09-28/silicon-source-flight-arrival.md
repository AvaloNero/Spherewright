# Same-star silicon-source flight and arrival

Local live observation, 2026-09-28. Source baseline `bfd328a`, installed
cohort `034300e`, native DSP `0.10.35.29088`. No tag or release was made.

## Flight, checkpoint and normal save

The kit window was sealed after audit, push and green CI. A fresh preview
reported `prepared=true`, no blockers, distance61221.4075m and an estimated
9037 game ticks. It did not expose an energy-budget DTO. The approximately
2.121GJ available versus1.2GJ required was a source-and-snapshot calculation,
not a public returned budget or a promise of return-flight fuel.

Luna then ran one independently reviewed fixed flight-plus-save suffix. It
used a new native prepare, not the preview token. Protected action evidence
is under prefix `555e043e0aa949d1b3e1c26403a133f6`:

- Flight `23b536ee-c8c7-4abf-91d4-83457d21d8ca`: accepted once, not replayed;
  native checkpoint75917679, flight75917679→75920802, succeeded without
  recoveryRequired. The native terminal reports living, grounded Walk on
  planet102 for600 verification ticks.
- All16 backpack entries retain their quantities and inc. Loaded graphite
  decreases195→186 through normal use. Landing core energy is only15.53MJ;
  subsequent movement/construction requires a new energy read.
- Normal save `f4eccf8f-039c-485d-8460-31f2334e0ef1` succeeds at75920947/R32.
  The final session is owned/healthy/saved on102, restart capability is
  available, and the flight reload capability is absent. Journal91 entries
  remain equal, durable through91, without pending persistence or error.
- Root separately reads only the source-defined installed checkpoint record,
  verifies stable before/after fingerprints and the exact session/action/
  origin/destination. Its lifecycle is **retired**, checkpoint tick75917679,
  successful-flight tick75920802, retirement tick75920947. No token, save
  name or private path is emitted by this check.

Retirement proof: `f26af03720fe4ccab85a04b8dadf6ba8/0001`, SHA-256
`58215F71DD47E18433AC4B34AA19C466D9F142DE59ED9522BF235D8EB420B834`.
The current external window contains2 accepted actions, not a ten-write reset.

## Arrival baseline, not a repair

Root performs one audited seven-read arrival collection: session, two built
pages, empty prebuild page, station44 detail, power, session. It finds179
entities,320 directed connections and zero prebuilds, with stable
R32/save75920947. This establishes a new102 snapshot; it is not a comparison
proving that an older remote factory never changed.

Station44 is the interstellar station, full12GJ and service ratio1 at this
snapshot. Its titanium slot is200/200 and silicon slot91/500, both remote
Supply; orders and resident vessels are0. Mother-planet vessels can fetch
supplies, so zero resident vessels does not establish a defect. Miners1 and17
advertise respectively2 and4 resource node IDs. These observations alone do
not prove the silicon bottleneck, justify another miner, or establish a
sustained rate. Existing belt/storage indices must retain native semantics.

Arrival proof: `f159e351c6d84aaebe92514f7cb83cde/0008`, SHA-256
`6B07CE667DB1714EA4575F5000DF7773A64BAEB06A1ADFCED95781C468CEF628`.
An earlier collector's unlogged, locally failed summary is excluded from
evidence. Its guessed `droneCount` field cannot support a game-state claim.

## Caller-output correction and remaining boundary

The executed private flight caller serialized an entire action result, which
can contain a checkpoint reload token. Protected raw files were already
redacted, but that does not sanitize separate stdout. After the run exited,
root routed both displayed results through the existing redactor; a synthetic
token case and the pre-import no-Execute rejection pass with zero game calls.
The corrected caller was **not** rerun. See [IFX-181](../../incident-fix-log.md).

No remote construction, station change, refuel, return flight or later
restart is performed in this stage. Next: trace the observed silicon miner,
output path and station dispatch conditions, with fresh energy before writes.
