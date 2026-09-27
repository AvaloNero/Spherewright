# Bounded hydrogen withdrawal, saved; sustained recovery not proved

Local live, 2026-09-28, `owned-world-001`. Documentation parent `1a2490a`;
installed `034300e` / DSP29088, unchanged product binaries.

The existing coupling diagnosis suggested testing a small release of hydrogen
backpressure before constructing an approximately200m independent graphite feed.
The selected intervention was normal withdrawal of20H2 from existing store3064,
reserved for subsequent ordinary mecha refuelling. It is a finite intervention,
not automatic supply or a completed persistent fix.

## Movement and exact unexecuted suffix

A23m approach preview exposed water risk and was never committed. A different,
previously observed Walk destination passed native prepare; its approximately141m
route was outside the bounded surface preview, so route safety remained unknown.
The one committed Move ended `position_stalled` at76473661 after180 stagnant
ticks, reporting2.5899m remaining and `doNotRetrySameTarget=true`. It remained a
failed action and counted as external accepted8.

Fresh readback subsequently showed Walk/zero speed,618.414MJ, unchanged complete
inventory, a position within0.05m of the historical destination, and an allowed
business transfer preview. The move was **not** retried or relabelled successful.
An independently reviewed suffix bound that exact failed action and R61, skipped
all movement requests, and freshly prepared only the unexecuted withdrawal/save.

| Action | Actual terminal evidence |
| --- | --- |
| Withdraw20H2, accepted9 | tick76485900; source3064 instant600→580; player2→22, inc0; other backpack groups unchanged |
| Normal save, accepted10 | tick76485932; R63/healthy, original durableJ91; new writes frozen for audit |

The source's30 unbanned default slots each held20H2 before the transfer. Its later
stock was581 because its existing automatic inlet had replenished it; the immediate
terminal600→580, not cross-tick subtraction, proves the transfer. Existing static
storage configuration and reciprocal connections were preserved.

## Evidence and limits

The single closing audit collected52 entity pages from one snapshot, preserving
all5160 mother-planet entities and10062 reciprocal directed edges, with zero
prebuilds. OriginalJ91 entries/version transitions and healthyR63/save76485932
were retained through audit tick76490087. The earlier seven accepted actions
remain covered by the separate [silicon repair/save](silicon-parallel-outlet-save-audit.md)
and [return/save](silicon-return-mother-save-audit.md) closures; none was replayed.
This latest collection is on104, not a fresh remote102 factory collection.

Audit proof `94cf5f6e4e534a71bdf134b62538b1cd`/0059 SHA-256:
`A3095190504B5F18F56FF80662AE67F06EC93838F827DDD15056C881C42B4260`.
Its observer initially discarded the command handle and reported incomplete
progress; the original process completed all58 reads and the proof. No successful
page was recollected and no gameplay action was repeated.

- Failed movement: protected prefix `3b588da5e3c040f2bee9f69f183e36d2`, terminal0025;
  subsequent zero-write business preview/player evidence `10616c84b18e4140bdfddada68e673a3`.
- Suffix: `ec6784a4041246c2b28ca25e6059adab`; transfer0012 SHA-256
  `0B08F7B6ED686B0DA63AE08ECF6185340159199620BDA85DF88FFD7E6EC0F9C4`;
  save0019 `E68389D5CE89735A428FF8AB5EF52D6AA0AC8CA9E467D9DB58A60F39874EBB21`;
  proof0022 `11BCEBA9140BECA0EA9D8DF2B46C6FD68527C74925AAF91F3E59BBB9F43E9679`.
- The independent post-withdrawal600tick window76488104–76488703
  (`0afdd2cbc69c4bcd875f1ce83ae6aea0`/0002) sees graphite2 produced/0 consumed
  and H2 7/4. D, rings, rods, CNT, broadband and purple are all0/0. The target
  upstream diagnostics still report hydrogen-blocked3083/3084. The planet-wide
  graphite count is **not** attributed to a particular cracker.

The executable entry's parser and actual no-Execute file guard passed offline.
The unchanged MovementProgressWatchdog/MovementSurfacePreviewPolicy regression
passes37/37; this does not prove the route was collision-free or production
restored. No refuel, new factory entity, restart, continuous throughput or final
version gate is claimed. Next, after audit commit/push/green CI and explicit
workflow handoff: use the staged fuel normally, and test the dependency without
repeatedly clearing storage.

## Subsequent negative result and ordinary fuel use

The staging/audit closure was pushed as `c167eaf` and CI36353041129 succeeded.
The external ten-action window was explicitly sealed; game revision and ticks
were not reset. A later independent600tick window76526024–76526623
(`82fd488311d9455cb2a0f3abd7c594ba`/0002) has zero production/consumption for
all nine selected items. Fresh3064 is full600 again; ring3404 has magnets6,
turbines4 and graphite0, while rod3403 retains alloy2/D40 but rings0. Thus this
20-item withdrawal did not establish sustained recovery. The unsampled interval
is not asserted to be continuously idle. Do not repeat the same withdrawal.

One ordinary refuel then moved20H2 from the backpack22→2 into the empty fuel
chamber0→20 at76528518. Subsequent readback has chamber19/reactor item1120,
showing entry into the native reactor path, not a sustained consumption rate.
Normal save76535143/R65 follows, with unchanged originalJ91 and healthy writes;
the new external window contains two accepted actions. No new factory audit or
restart after this refuel is claimed; the earlier104 audit remains a dated
snapshot. No building, recipe, connection or station configuration was changed.

A later three-read packet `ae422baad37a4297abbc03fb2be876bc` retains the saved
R65 boundary: player core800MJ, fuel chamberH2=7, reactor item1120, backpackH2=2.
Its independent window76554428–76555027 again has0/0 for the nine selected
factory items. Factory statistics are not a ledger of mecha fuel consumption;
this finite fuel use does not establish a continuous factory hydrogen sink.

- Refuel: `e171de1a7edd498db31adf32137d9688` terminal0006 SHA-256
  `AF92AE0C261EA7FFC93D32BB904A7324ED12F29F59B45CD52836619B814906EC`;
  proof0009 `7D3700C61D7D492ADC5EC8D30107068DC4342AEA24AC32EB3B176526763D8119`.
- Save: `30a2f04c70814563a13a45cb9479a5f2` terminal0005
  `791E1269DA83587C9ED163B11CC47E58D032978FBF6D8DA447C0CA9C6535C005`;
  proof0008 `B0DE5ADBBA723CFE8393D900C573D8BCB72F0D5CD279F99BF0B6827DFEC4BCF6`.

Next is a bounded permanent supply design, including existing coal-graphite
conveyor routes and available station slots. The current MCP already supports
station storage/output/charge modes inside `prepare_configure_building`; absence
of a separately named station-config tool is not absence of that capability.
Unoccupied slots do not prove that either endpoint or a complete route is usable.
No new route is approved by this observation.
