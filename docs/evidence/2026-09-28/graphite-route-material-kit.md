# Graphite-route material kit: normal crafting and save

## Result and boundary

One bounded material-kit stage completed with two ordinary warehouse transfers,
three ordinary handcraft batches, and one normal save. The player now carries
400 basic belts and 5 basic sorters. This proves the recorded inventory change
and save only; it does not prove a graphite route, a working interface, or
continuous production.

No build or configuration action was submitted. The two source warehouses'
static configuration and connections stayed intact; their item counts changed
through the transfers. No whole-factory static audit was performed in this stage.
No new coal supply, graphite
throughput, or sustained output is claimed.

## Six terminal receipts

| Step | Terminal result | Observed inventory effect |
| --- | --- | --- |
| Transfer iron | completed at tick 76694233 | player iron `0 -> 377` |
| Transfer circuit boards | completed at tick 76694303 | player boards `0 -> 5` |
| Handcraft gears | recipe 5, 124 batches, 76694423–76699384 | iron `377 -> 253`; gears `0 -> 124` |
| Handcraft belts | recipe 84, 124 batches, 76699490–76704450 | iron `253 -> 5`; gears `124 -> 0`; belts `28 -> 400` |
| Handcraft sorters | recipe 85, 5 batches, 76704540–76704740 | iron `5 -> 0`; boards `5 -> 0`; sorters `0 -> 5` |
| Normal save | completed at tick 76704796 | no item delta |

Each terminal was `completed`/`succeeded`; none required recovery or
reconciliation. The final player read has an empty handcraft queue, stopped
movement, and preserves the other inventory outside this scoped kit.

## Save and Journal tail

The post-save session is revision 74 at save tick 76704796, healthy and owned.
The external accepted-action count for this window is 8. The final Journal tail
reports durable-through sequence 91, with `persistencePending=false` and no
`persistenceError`; original entries and version transitions are unchanged.
All backpack item inc values are preserved. Fuel-chamber hydrogen changed7→3
during normal crafting; fuel invariance or sustained factory hydrogen use is not claimed.
These receipts cover the kit save; they do not establish a
later restart/resume result.

Protected run `7bc07e8aaeec4013adb931f517aa43e9` contains the six terminals at
0010/0020/0072/0126/0139/0148, final player0149, session0150 and Journal0151.
Sol independently verified the same-action receipts, both source-store deltas,
unchanged source configuration/connections, complete backpack counts/inc, and
Journal continuity. Save0148 SHA-256:
`39A90617170D21F3CD3D678CF25EEFD05898E98922884FA9EB686DA38F634585`;
Journal0151: `23FCFCE6093AA87C694E315E0384B50BF2C567D1DC06B4971F30789625236314`.

## Local entrypoint learning

An earlier caller preflight stopped after two reads and before any commit when
an empty `Measure-Object.Sum` path was evaluated. Root replaced the two affected
helper accumulations with explicit `long` accumulation. Six offline helper
cases and eleven pure guard checks against the actual DTO inputs then passed.
This is evidence about the local caller's budget and guard handling, not a
claim of a new game tool, Plugin repair, or product-level capability.

## Remaining gates

The material kit is only portable inventory. Fresh, native checks are still
needed for any movement, placement, conveyor/sorter interface, whole-route
cost, coal allocation, station use, actual graphite delivery, and sustained
production. The separate [coal-graphite capacity gate](coal-graphite-capacity-plan.md)
remains unchanged.
