# Iron ore feeder upgrade — R340

R340 independently audited the saved iron-feeder upgrade. The audit is `28262a6a81c84d3ca0a0f26d4f491984:1` (SHA-256 `34181F4F9572482298B3329EBAE1A3E265A09652091AD8DA56776523577AE1F1`). The R338 writer receipt is `c3c1e9cfb5f74f8dbe5081f134ec7399:54` (SHA-256 `127734B570819E1F5FB475CAC3369127A1E5C613F6479B9ED538A39494D11CC9`).

The R337 read-only baseline (`87e9592c55354a4a96105e6f14870ac8:13`, SHA-256 `4A589F7258F7C61482054983DD729A4A11E67601C556A26D883B696D750ABC8C`) covered the relevant 22-object Native path. Before the upgrade, sorter `6274` was a basic sorter using filter `1001` on the `6273→1507` path; its listed `progressRequired` was `600000`. The same baseline read the affected iron nodes, furnace endpoints, and local power. These point observations are a baseline, not a production-rate measurement.

R338 performed only the Native upgrade of `6274` to a fast sorter and a normal save (2 accepted actions, 43 requests, 7.4 seconds). R340 verified the resulting object still has ID `6274`, uses `2012/filter1001`, and contains one `FeOre` while preserving cargo count/increment and both reciprocal connections. The prior basic-sorter cycle ratio and nine related static fields were preserved; the affected local grid remained full-serve. No sustained output or supply rate was established.

The saved stage ended at R50/J102, normal Save `101987980`, external `13`, lifetime `353`, with the fixed 20-write window frozen and no in-flight or unknown actions. The player's net material change for the stage was `2011 +1` and `2012 −1`; the final inventory has two basic sorters and no fast sorters, with the other kit items unchanged.

This closes the upgrade and its saved readback only. R323/R324 still records `sourceConditionsPassed=false` and continuous credit `0`; whole-supply readiness remains unpassed. The next work is a bounded read-only assessment of the iron-inlet effect and actual flow through the existing shared oil/graphite branch.
