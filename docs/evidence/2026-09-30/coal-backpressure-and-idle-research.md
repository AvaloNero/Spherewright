# Coal backpressure and idle research — bounded diagnosis

Input boundary: same healthy owned world on planet `104`, previously saved at tick `78497715 / R22 / durable J91`. This is a read-only diagnostic plus two native `prepare_build` rejections, not a production repair, construction commit, restart, or version gate. The exact protected Bridge responses remain local; no save name, token or raw transcript is published here.

## What the fresh data says

The ready `600`-game-tick Overseer window `78553842–78554441` reported produced/consumed counts of `0/0` for coal `1006`, graphite `1109`, plastic `1115`, hydrogen `1120`, red matrix `6002`, and yellow matrix `6003`. Their respective theoretical production rates were nonzero (`462`, `210`, `20`, `225`, `10`, `7.5` per minute), so a theoretical-capacity number is not evidence of current throughput. Coal miners `106` and `5162` were output-blocked with `50/50` output. Existing graphite furnaces `113`, `2719`, and `2720` each had `100/100` graphite output, and warehouse `114` was full at `3000/3000` graphite. The new coal path has coal in its miner, sorter and belts but currently ends without a consuming device; its local incompleteness does not explain why the old graphite production is also backed up.

The same bundle reported `currentTechId=0`, `queuedTechCount=0`. A separate protected progression read at tick `78558743` confirmed an empty queue. Lab `84` at tick `78581483` was powered (`serveRatio=1`) but not working, with `36000` blue, `36410` red, `36000` yellow research points, and `0` purple; its matrix inputs remain physically attached. Tech `1125` is locked with its prerequisite `1124` already unlocked and is on the path `1125/1126 → 1141 → 1303 → 1705`. This makes normal research selection a bounded demand-side test; it does **not** yet prove that all blocked chains will recover.

Before the demand check, two electric-furnace site prepares were rejected: one `BUILD_LOCATION_INVALID / NeedGround`, the other `BUILD_LOCATION_INVALID / overlaps factory object 353`. Neither yielded a construction commit or changed the world. After the second rejection, no third furnace site was tried. A new furnace would not resolve full existing graphite outputs, so that supply-side candidate is retired pending measured demand.

## Native demand-side test and save

Main selected tech `1125` because its prerequisite `1124` was unlocked and it is a prerequisite on the `1141→1303→1705` progression path. Luna used the existing protected action client. Fresh session/player/progression/lab reads reconfirmed the same owned world, empty research queue, available selection hash and lab `84` inputs. One native `prepare_select_research` was allowed without blocker; one unique `commit_select_research` yielded action `5052a88a-9aea-4087-8530-2f7081d5588c`, `completed/succeeded`. No other research was selected.

Raw protected progression and lab reads confirm the following two independent game windows (lab points are not item counts):

| Observation | Game tick | Tech `1125` hash | Lab `84` red points | Lab `84` yellow points |
|---|---:|---:|---:|---:|
| After selection | `78599942–78600049` | `16/240000` | `38534` | `38124` |
| Window 1 | `78600671–78600732` | `745/240000` | `37538` | `37128` |
| Window 2 | `78601339–78601390` | `1413/240000` | `36842` | `36432` |

The hash increases are `729` and `668`; both red/yellow point pairs decline. The first selection is Journal `J92`, actual time `2026-09-30 02:40:42.8014419 +08:00`, game tick `78599930`, game time `015d 03:53:18`. One later ordinary save, action `e651f40f-23d2-4842-ac8f-2245d3e9c06b`, completed at tick `78609977`; closing same-owned session was tick `78609992 / R25`, `saved/healthy`, protected resume available. Journal `92/92` was durable with no pending/error. External accepted progressed `3→4→5` for research selection and save. Luna reported `27.1 s` for the fresh research chain plus two windows and `1.5 s` for the separate save; these exclude root analysis, documentation, Git and CI.

A **single** later ready production window `78614765–78615364` reported coal `13 produced / 10 consumed`, graphite `5/8`, red matrix `2/2`, and yellow matrix `1/2`, where all four had `0/0` in the earlier ready window. This is a positive demand/production pulse, not yet a multi-window sustained rate, proof of complete yellow supply, or post-save restart. No factory construction, transfer, handcraft, flight or additional research selection occurred in this stage. Protected local evidence is indexed by the `action-3702113779af43fd8e3eafa06f30b5bf`, `action-c7cf09994c1d43fd91e102e92146eacf` and `action-7ef60bf461454892bd302f14ef6f77da` receipt groups.

## Repeated, separated production windows

Two more read-only `get_overseer_production` results each used native factory statistics over exactly `600` game ticks; their intervals did not overlap. Main independently checked the protected production replies rather than asking the game to resample them:

| Ready window | Coal `1006` P/C | Graphite `1109` P/C | Hydrogen `1120` P/C | Plastic `1115` P/C | Red `6002` P/C | Yellow `6003` P/C |
|---|---:|---:|---:|---:|---:|---:|
| `78632310–78632909` | `13/14` | `7/8` | `2/5` | `2/2` | `2/2` | `1/2` |
| `78642360–78642959` | `14/12` | `6/6` | `2/3` | `2/2` | `1/2` | `1/2` |

Here P/C means produced/consumed item counts within that window; it is not a full material balance for the intervals **between** windows. Fresh progression at tick `78644445` still had tech `1125` queued and unlocked=false, with hash `44519/240000`. Lab `84` was working at full power with red/yellow points `37550/37140`. Graphite store `114` had `2962/3000` across all 30 slots; furnace `113` was working with 4 coal input but still had graphite output `100/100`. Thus demand has restarted some production without yet freeing every output buffer or proving a long continuous rate. Separate infrastructure findings reported four depleted veins; the six target production entries in the second window had no findings, so the vein alerts are not assigned to this chain without tracing their actual entities. No game writes occurred in this follow-up; external accepted remains `5`. The two production receipts are the local protected groups `action-4245394d0fc74cd8a7599c63dd3cc3ca` and `action-c25047d9ca884d92871d1cffb326ce1e`.

Next: test the lasting hydrogen sink and yellow-chain inventory trend; if a specific branch re-blocks, trace that endpoint before increasing upstream capacity. A fresh protected restart is still unproved for this research selection. Do not treat queue nonempty, temporary buffer loss, one inventory pulse, or nominal machine capacity as sustained output.
