# Research-demand save and protected-resume production check

Date: 2026-09-30. Scope: the same owned world on planet `104`, after the normal selection of technology `1125`. This is a persistence and bounded-production check, not a continuous-supply or 0.4 readiness pass. No different save, LastExit, autosave, import, flight checkpoint, direct game-memory write, or inventory injection was used.

## Normal save and exact-primary recovery

- Before this stage, the last primary save was tick `78609977 / R25 / J92`; fresh preflight showed owned/healthy, no blocker or quarantine, player `Walk/0` with an approximately `800 MJ` core, and Journal `92/92` durable with no pending/error.
- One ordinary save action `8ef189c8-1e15-431f-a1de-715ee7d93f37` was accepted once and terminally succeeded. Fresh readback showed primary tick `78824574 / R26`, healthy writes, durable J92, and a protected planned-restart ticket. This was external accepted write `6` in the current window.
- The bound DSP process closed normally through its main window in about `11.5 s`; it was not killed. The live descriptor count reached zero. A new process was started through the installed Steam client using the established app launch, not by directly starting the game executable.
- At the main menu, one live descriptor reported `gameLoaded=false`, healthy writes, and the newly issued restart capability. A first default exact-primary prepare was not committed because its short-lived plan token stayed in a completed caller process; it had no game side effect. A fresh default prepare and unique commit in the same caller then produced resume action `937caf79-68de-4723-aac2-54474ce1744a`, terminal `completed/succeeded` at tick `78824606`. Fresh state at tick `78824629 / R1` was loaded/owned/saved/healthy with last primary tick `78824606` and a new restart capability. This was external accepted write `7`; the uncommitted prepare was not counted or replayed as an action.
- Correct fresh `get_gameplay_journal` readback retained durable J92 with no pending/error. Research readback on planet `104` still had current technology `1125` (Casimir Crystal) and one queued technology. Two caller-only read mistakes—a player request initially lacking `planetId`, and an unavailable `get_journal_state` method—returned read-only rejections and were corrected without action replay. Neither is evidence of native save corruption. Root separately re-read the installed Bridge after the stage: protected evidence run `ec41e31d1a10478698a5fe0602b87b33` records owned/healthy planet104, tick `78868595 / R1`, last primary tick `78824606`, and Journal durable-through `92` with no pending/error. It is a later state read, not a replay or same-tick proof of the production windows.

## Two independent post-restart windows

Both native production responses were ready and cover separate 600-game-tick intervals. Values below are produced/consumed counts, not capacity or continuous-rate claims; every item identity was checked against its returned runtime name.

Root independently re-read the protected raw production responses `action-a31311f624ff4b638fd2e4e5a63f6b64-0002-bridge-response-get_overseer_production.json` and `action-6beb41d6c1b149ceb343582d9d84ca1f-0002-bridge-response-get_overseer_production.json`, confirming both ready windows, exact item names and all six produced/consumed pairs below. The saved action IDs and root's later protected session/Journal run above are separate evidence; object `3064`/`774` details below are the executing Agent's direct readback, not a root same-tick replay.

| Item | Window A `78838782–78839381` | Window B `78846109–78846708` |
| --- | ---: | ---: |
| Coal `1006` | 12 / 14 | 9 / 10 |
| Energetic graphite `1109` | 7 / 5 | 5 / 6 |
| Hydrogen `1120` | 3 / 3 | 3 / 3 |
| Plastic `1115` | 2 / 2 | 3 / 4 |
| Red matrix `6002` | 1 / 2 | 0 / 0 |
| Yellow matrix `6003` | 1 / 2 | 1 / 0 |

The intervals do not overlap, but their gap is not counted as a qualified continuous window. Storage `3064` remained a full small storage, 30 slots × 20 hydrogen = `600/600`, not a fluid tank, at tick `78849281`. Lab `774` was on yellow-matrix recipe 27, working at powerServeRatio `1` in network 3, with diamond `1112 × 6`, titanium crystal `1118 × 6`, and yellow output `0` at tick `78849290`. The snapshot does not identify hydrogen's final sustained consumer; the observed hydrogen consumption can include recycled refinery input. Nor does a working lab or one yellow unit per short window prove durable yellow throughput. Red-matrix `0/0` in window B is a new exact branch for diagnosis; the evidence does not establish its cause.

Conclusion: ordinary save, clean exit, exact-primary protected resume, Journal/research continuity, and two bounded post-restart production observations passed. Sustained hydrogen disposal, red/yellow balance, a full continuous production window, remaining Foundry/readiness gates, and a final package do not follow from this stage. Do not repeat the successful save/resume just to inspect these gaps; next use targeted current-consumer and red-line reads before any construction. The current external accepted count is `7`, so the ten-write freeze has not been reached.
