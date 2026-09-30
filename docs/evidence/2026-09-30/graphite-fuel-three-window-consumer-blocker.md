# Graphite/fuel: three independent native windows and the next consumer boundary

Date: 2026-09-30. Source `512e73a`, CI36678094539 passed. Same running owned world, planet104, DSP0.10.35.29104; no deployment, load, movement, save or construction. Existing Luna's previous read-only handle83772 had terminated before this explicitly approved new experiment. New handle75294 terminated successfully; no in-flight action or unresolved commit. Accepted delta0, external window remains4/10.

## Original evidence and bounded execution

Protected run `ebf65923d61d4025891f3161c1661267`: entry boundary0001–0006, intent0007; fixed10 entities/8 items, three independent600-tick windows, poll5 wall seconds, original180-second/90-request cap. Result0076 reports65 sampler reads,120000ms scheduled waiting,4930.332ms measured reads,125507.5177ms total. These are caller timings, not physical construction time or model/token measurements. No per-window model decisions or writes. Old failed-run windows are not joined to this experiment.

Root independently read the original selected-entity, power, production, session and Journal receipts, rather than issuing another sample or relying on stdout's last-rate summary.

| Native window | Original read ordinals / derived event | Coal1006 | Graphite1109 | Rings1205 | Fuel1802 | Hydrogen1120 | Oil1114 | Red6002 | Yellow6003 |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
|80178659–80179258|0017–0029 /0030|9/8|12/11|0/0|0/0|29/29|10/10|1/0|0/0|
|80179299–80179898|0039–0051 /0052|9/8|12/16|0/0|**2/0**|27/31|6/10|1/0|0/0|
|80179942–80180541|0061–0073 /0074|10/6|10/9|0/0|0/0|27/24|12/9|1/0|0/0|

Values are produced/consumed **items per600 ticks**, not per-minute rates. All three windows are ready, complete, nonoverlapping and do not cross session boundaries. Gaps are not continuous credit: `coveredGameTicks=0` is expected in this independent experiment. Planet statistics do not attribute output to a particular new source. The middle fuel window really contains two produced rods; absence from the terminal summary cannot turn it into zero or erase the sample.

## The consumer changes the next action

- Furnace5187, recipe17, all three entity reads0018/0040/0062: coal4, graphite output100, powerServeRatio1, not working at those instants. Output sorter5196 is observed working/carrying graphite. Belt5189/5193 and receiving old belt3365 contain graphite; the selected cargo scopes cannot be summed into a whole-route inventory or counted as unique delivered items.
- Ring assembler3404, recipe103, reads0025/0047/0069: turbine4, magnet6, **graphite2**, ring output10, full power. Its output connects to3416 and fuel assembler3403; all three snapshots are not working. Thus “no graphite input” is no longer the evidenced current blocker. The zero ring windows plus retained output suggest downstream throttling; the exact capacity/consumer cause still requires the next bounded native diagnosis. Do not add another graphite source or redo5193–5196.
- Fuel assembler3403, recipe41, reads0026/0048/0070: titanium alloy2 and ring2, deuterium5→10→15, output0; first working, later not working, powerServeRatio1. Runtime catalog0004 requires alloy1/deuterium20/ring1→fuel2. This establishes a specific low deuterium-input branch to trace, not its remote producer cause or a proven sustainable rate. Its input neighbours are3944/3405/3416 and output3414; fresh verification remains mandatory before any write.
- Hydrogen storage3064 remains600/600 in0027/0049/0071. Power receipts0028/0050/0072 show networks3/4 consumerRatio1. Hydrogen P/C includes potential recycling; neither that equality nor red output proves a lasting useful sink or restored yellow chain.

## Identity, research and evidence limits

Final session0075 at80180543 is owned/healthy, revision22, primary save still80012591. Journal0077 at80180545 retains durable95, pendingfalse/errornull. No save/restart validation occurred in this experiment.

Progression0005 at80178582 has no active/queued technology;1125/1126/1607 are unlocked. Native2104 is still locked and requires500 each of6001/6002/6003/**6004**, and2904 is still locked with prerequisites1704/2104/2903. Catalog0004 already unlocks information-matrix recipe55 (processor2+broadband1→6004), warp recipe78 (lens1→1210), and deuterium recipe40 (hydrogen10→deuterium5). This is a real remaining preparation dependency, not permission to claim warp readiness, inject research or fly across stars.

Next finite stage: diagnose the existing fuel deuterium inlet and current red/yellow consumers, using their actual endpoints and native findings before choosing any upstream repair. No unchanged rejected geometry retry, no source rebuild, no new action primitive or type. Full supply, original Foundry three-level construction, preparation quantities/rates and final packages remain unproven. The historical copied-module/cancel/resume and original31→62 throughput acceptance on2026-09-10 remain separate evidence in the save diary, not claims about this diagnostic or the current DLL's complete release regression.
