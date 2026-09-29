# Historical-Walk approach, coal sorter upgrade, and owned save

Scope: one bounded access Move, one native single-sorter upgrade, and one ordinary save in the same healthy owned world on planet `104`. This new external window began at accepted `0` only after the previous ten-write audit was committed, pushed, green in CI, and sealed. The previous primary save was tick `78311012 / R17 / J91`.

## Access without replaying the stalled local targets

The target was the same-world historical settled `Walk/0` point `(-7.75039625,-87.29358,-180.011673)`, read from tick `77509295`; an earlier normal `95.69 m` Move from that point back toward the factory had completed. From the current stable point the new arc was about `88.9 m`. Fresh `prepare_move` allowed the exact target with no blocker but correctly marked the full surface preview `unavailable` because it exceeded `32 m`. A separate read-only `12 m` probe observed all 13 terrain samples and entered shallow water after about `4 m`; the historical endpoint and approximate full-factory non-belt clearance review were **not** proof that this new route had no collision or water risk. Root authorized exactly one attempt under the existing historical-Walk exception; no failed earlier target was retried.

Move action `a5a00573-28fd-4f68-9141-f95cf78efc7a` reached `completed/succeeded` at tick `78458326`. A later settled player read was `Walk/0`, `1.013 m` from the fixed target, with `783.916 MJ` core energy; session read was healthy at `78458449 / R19`. No second route attempt or position write occurred. This is one successful route on this world, not an automatic pathfinding or general water-crossing guarantee.

## Normal sorter upgrade and durability

From the new position, fresh player/target inspection and native `prepare_upgrade` allowed object `5170`, basic sorter `2011 → 2012`, while preserving recipe `0`, coal filter `1006`, and the two reciprocal links to belts `5163` and `5167`. The budget was one `2012` consumed and one `2011` refunded. The unique upgrade action `90fa83fc-39a0-48c4-ba1c-07db1a2d70c8` was terminal `completed/succeeded`; the resulting object remained `5170`. Fresh readback showed `2012: 2→1`, `2011: 2→3`, the sorter-held one coal item and its insertion state/stack preserved, both reciprocal links and filter retained, native cycle fraction retained (`0/600000 → 0/300000`), and sorter power serve ratio still `1.0`. Closing session was healthy at tick `78484595 / R21`. No claim is made about the endpoints' power fields where their DTOs were null.

An ordinary save, action `6be64202-18af-420d-a499-925d8a77da6a`, then completed at tick `78497715`. Fresh session read was healthy, `saved`, no save error, and protected restart resume available at game tick `78497727 / R22`; Journal remained `91/91` durable with no pending persistence/error. These three actions account for external accepted `1–3` in the new window. No restart after this save, full-factory static audit, sustained coal delivery, graphite output, hydrogen recovery, or 0.4 gate is claimed. The exact protected raw Bridge receipts remain local, not in the repository.

Next fresh business step: inspect the coal endpoint and continuous flow, then plan a powered, budgeted coal-to-graphite consumer through existing native build primitives. Do not equate this faster sorter or a full belt with sustained graphite production.
