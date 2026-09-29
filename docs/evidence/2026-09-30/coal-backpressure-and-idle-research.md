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

Next: compare independent production windows and the same affected buffers while research runs. If consumption continues but coal/graphite becomes truly supply-limited, revisit the complete supply/transport budget. Do not treat queue nonempty, temporary buffer loss, one inventory pulse, or nominal machine capacity as sustained output.
