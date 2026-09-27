# HPS supply diagnostic windows

## Four bounded observations

After the two remaining infeed attachments and normal save, four independent,
read-only Overseer bundles sampled the current planet104 chain.  Each is one
ready 600-tick window; none is a production acceptance window or authorization
for a repair.

| Bundle receipt | Window | Direct observation | SHA-256 |
| --- | --- | --- | --- |
| `125b` / `0003` | `75427440–75428039` | Silicon ore1003 `0/20` and HPS1105 `10/9` items, or `0/120` and `60/54` per min | `823D3CA498C3AAAD82B09EFEFCE98323E166221611B79496CBC5C88293D911FE` |
| `7eb` / `0002` | `75449802–75450401` | Broadband1402 and information matrix6004 both `0/0` | `BFE8B8CA9C9992DD08CD841CE8B068146EC48A65971619D1834A73BA55C44D5E` |
| `23ef` / `0002` | `75472593–75473192` | Lattice silicon1113 `0/2` (`0/12` per min); broadband1402 `1/1` and matrix6004 `1/0` (`6/6`, `6/0`) | `E502F4C9AA0616927FC067921D069A8ACA8ED62969244DC94A509574CCB24FAE` |
| `372e` / `0002` | `75484866–75485465` | The requested1003/1105/1113/1302/1402/6004 chain is `0/0` throughout | `3C8932E9D90FDF0398DA78610FF6A00DD7AE1532A4385FD66DF6F91278DA300A` |

The `23ef` pulse proves that the route can deliver one observed broadband and
one observed matrix in that particular window.  It does not turn the earlier
or later zero windows into a stable rate.  Likewise, the first HPS rate of
60/min is a single measured window, not the requested long independent
acceptance windows.

## Directly observed current gate

In the final `372e` window both HPS furnaces842 and5115 are powered on
network3 with `powerServeRatio=1`, but each has input1003=0 and output1105=0.
Their associated1105 inserters4679,4678,5156 and5157 are all `Picking` with
stack0.  Lattice furnace101 is also fully served but idle with input1105=0 and
output1113=0.  This establishes the immediate local gate: silicon ore is not
reaching the two observed HPS furnaces in that window.

The preceding `23ef` finding at842 is a confirmed `material_shortage`: it
records input1003 available0 versus2 per cycle.  It also records configured
logistics, one carrier, no outstanding order, and source inventory242 only as
the configured-route supply total.  Its route dispatch state is explicitly
`unproven` and the upstream trace stops at `stocked_logistics_boundary`.
Therefore this evidence does **not** prove a cross-planet transport failure or
name a remote logistical root cause.

Storage843 still exposes100 lattice silicon across its bounded filtered layout,
while the later r36 broadband machine2255 temporarily held four lattice silicon
and ran during the `23ef` pulse.  These are point-in-time buffers, not a
promise that upstream supply, power/fuel, or matrix output will remain
available.

The first bundle's network3 report had consumer ratio1.
Every inspected powered device in the later packets likewise
reported `powerServeRatio=1`.  This rules out an observed local power-service
shortfall in these reads; it does not prove long-run generation, fuel or
network headroom.

## Boundary

The earlier fixed-AutoSave0 recovery and the three persisted HPS infeed
sorters remain independently evidenced in
[the recovery record](fixed-autosave-recovery-live.md) and
[the attachment/save packet](hps-remaining-infeeds-save.md).  This diagnostic
does not reopen them and adds no action. Under the existing continuation
authorization, the next step is a bounded read-only investigation of the
logistics route. Any later repair still needs its own finite plan and fresh
native preflight—not an inference from a single zero or pulse window.
