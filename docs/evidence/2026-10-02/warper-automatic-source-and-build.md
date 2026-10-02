# Warper 自动供料与建造准备

记录：2026-10-02（Asia/Singapore）。本文保留前置入口/材料事实，并记录当前有限接口施工封窗；后者不是完整自动供料、整链产能或重启验收。

## 前置入口与材料窗口（历史封存）

此前窗口从 0/10、lifetime accepted 23 开始，10/10 冻结、lifetime 33。runs `0efb2a182699476396adf21bb0c4c961`、`d78ba66493824f3985229e0c3c1ccd0a`、`beed2d28735243ba9bc5e8d5dc2516b4`、`b72af1dce67d43cfb3630c58e337370c` 合计十个唯一 resolved/completed action，0 replay、unknown、unresolved 或 in-flight。`b72af1...` 的六项包含四次 Transfer（两种物料各取、放一次）：item 1204×20 从 827 取出并放入 884，item 1104×20 从 26 取出并放入 884。884 从 default 改为 lock-occupied，两个输入格分别配置对应物料，其余格为 0，原三路输出保留。normal save tick 84852841 / R37，durable J97。

Root 独立审计 PASS：proof `action-22ffbb33c912411098ccdb40ae3df7f0-0001-warper-inlet-independent-audit.json`，SHA-256 `5B1850ABE88C0AEDDE35732BB1BC9E2DD04531200D4A0A28228C67F06F6C5363`（29.77 s，0 Game calls）。capture `cb17c8eb06bb4ae0b68494356042cc15` 为 54 页、5325 实体；全图 10386 条边均唯一互逆，无实体新增或删除。物品 count/inc 与原生 action deltas 守恒：Fe +292、Mag −4、Cu −2、Tesla +4，其余不变；power topology/capacity 不变、consumer ratio=1，prebuild=0；J97 身份和97条记录连续。

该窗口的设计上下文仍有限：旧北侧图 `c75e8d9b6bf1465ba42d1138cdc3abbd` 中 A3 为 `NeedGround`、B 已取消；power-context 不是完整覆盖证明。石矿 node 203 已由 miner 86 覆盖。只读 site 检查 `2332beaae1d14310ab68046d8efe15c8` 共17次：Lens/Warp `NativeOk`，Diamond 西侧及 object 2310 + Tower A 为 `NeedGround`；Tower/Storage 仅 prepare-only，未建实体。旧列距12布局 snap 后两 ASM bounding overlap，不能据旧布局或 prepare-only 结果宣称全链可执行。

## 前一轮有限接口施工封窗（历史）

外部新窗从 0/10、lifetime 33 开始。stage runs `02944d6232df44fe89801e6a27c5bb68`（先完成两项）与 `7d53ff4826d64af4bdc3224948441d48`（fresh suffix 八项）合计 10 个唯一 completed action；0 replay、unknown、unresolved 或 in-flight，lifetime accepted 43。首 run 完成 recipe 84 Craft×120（belt +360、Fe −360）及 collider 建造（item 2310 / object 5326）；suffix 配置 collider 5326 recipe 104，建造 Tesla 5327/5328、assembler 5329 并配置 recipe 78、Tesla 5330、stock 5331，最后 normal save action `871d078d-6293-4bf1-9990-cceaa2de75ad` 于 tick 84915771 完成。

Root 独立全图审计 PASS（26.61 s，0 Game）：proof `action-a3c3da97a47843b295eb202c04397030-0001-warper-construction-independent-audit.json`，SHA-256 `127F7D65DF39A87064BA02F21284AE6D31E2E05837D49EFC8AA50B36F16284FD`。capture `556086d58ba24144860466df5dad3995` 为54页/5331实体、0 prebuild，capture tick 84916675、closing observed tick 84917231；save/readback 为 tick 84915771 / R56 / durable J97。原5325实体静态配置及10386条互逆边全部保持；新增6实体无物流连接引用，其中两台配方机器正常接入 net3。net3 nodes 206→209、consumers 512→514，发电机与容量不变、ratio=1。玩家物料净额为 Fe/item 1101 −360、belt/item 2001 +360、collider/item 2310 −1、assembler/item 2303 −1、Tesla/item 2201 −3、stock/item 2101 −1；其余 inventory count/inc 不变。J97 identity 与97条 entries 逐字一致。

这只证明一次有限的未来接口施工 qualification。**Gate 2 整体仍未完成**：完整 executable 方案、自动供料、整链供给/覆盖、持续产量、实际保存后 restart 及 Gate 3 连续 36000 ticks 均未证明；Gate 1 虽已有 native 启动正例，持续 ≥1/min 仍未证明。此前的 prepare-only 或 NativeOk 只说明对应查询/位置边界，不代表生产链已运行。

## 石材来源、受保护恢复与当前封窗

本阶段在前一 owned-save 后完成一次中途受保护恢复及完整捕获；独立 proof `00ad7b5cfd7b4d629dbbc1879f402814` SHA-256 `16E3F627DCBC0163A34B453AC455937BFF55E9605051200BD5124275211423FA`。它只通过该次恢复子门，不代表最新 normal save 后已重启。首次恢复 prepare 因 `BRIDGE_NOT_READY` 停止、0 write，fresh 第二次成功。退役的 `SaveActionCache` 曾返回 `ACTION_NOT_FOUND`，随后按原 terminal 核销，没有重放。

object 95 的 Brick/item 1108 库存原为3000（满格），随后出现 Stone→861 acid→869 Graph 堵塞。一次100件 transfer 已成功；之后 entity 96 的 sorter 很快回填1件石材，令精确储位 prepare 被本地守卫拒绝，未产生配置 commit；成功前缀不重做。之后仅对既有输入做限量 guard（不是冻结所有 I/O），再 fresh 执行 58-unit transfer、预留一格 item 1005、恢复 bans=0 并 normal-save。受保护索引确认 run `122321492928498ebe0f85cdea360609` 的后缀五个唯一 accepted 完成、无 replay/unknown/in-flight；连同先前成功的一项常规 action 与一次恢复，本窗口共7个 accepted（6常规+1恢复），外部 7/10、lifetime 50。normal save tick `84935126` / R10 / J97 覆盖本窗口7个写入；其后自动生产引起的全部动态 buffer 变化不因此视作已保存。

完整 capture `0f1600f4bbd64c57bcbe262555c5c257`：factory tick `84939047`、closing observation `84940013`、5331实体、0 prebuild。root 独立全图审计 PASS（12.448 s、0 Game calls）：proof `action-fd59544676c1441a9390c1f90c0a326f-0001-stone-source-window-independent-audit.json`，SHA-256 `AFC767A8D85ED6E9637818EAEB6B35FEBA97317D3E0658A2E56838EF2DB0BA5B`。除 object 95 获准的 `storageConfiguration` 变化外，所有静态配置相同；10386条边互逆且不变；原三条连接保持；每次配置的同步边界上 native ordered-buffer count/inc 保持；player Brick +158，其余 inventory count/inc 不变；供电拓扑与容量不变、consumer ratio=1；J97 identity及97条历史 entries 连续。

直接采样 run `a3fedfa17f534abbb9364abda3a5a6f6` 的三个互不重叠600-game-tick窗口按窗计数（非速率）：planet 104 的 Stone/item 1005 产出 `5/5/5`、消耗 `0/0/8`；acid/item 1116 产出 `0/4/0`、消耗 `0/0/2`；Graph item 1123 产出 `0/0/4`、消耗 `0/0/0`。861 的 Stone 输入为 `7→5→2`、oil 为 `12→12→10`，miner 86 working、buffer `32/31/32`；object 95 的 Brick/item 1108 库存三窗均为2900。采样时869即时快照均为 `isWorking=false`、acid 0、Graph output 0；另一次同快照 detail 为 `isWorking=true` 且 acid input 1，这是不同tick的状态，不构成冲突。root随后用同R10/session的两项catalog只读记录与既有完整快照确认：runtime的1123输出配方31/32中，唯一实际 producer 为869/r31。归因 proof `action-c22be89a10a54788b8c58972aebcd3f6-0001-graphene-source-native-attribution.json`，SHA-256 `7F1E2768A62DF6FED1DEB5E878A1645526F026C0C5778ED47A912ED67FD5B135`（0额外 Game calls）。该采样62 requests、总墙钟284.370 s（scheduled wait 270 s、读操作13.884 s）；这是观察等待计时，不是持续产率证明。这只确认生产源，不证明自动送达883或完整自动供料。此前独立0产量负窗口不与这三个窗口拼接；以上不证明每窗非零、持续 ≥1/min 或稳定产率。

当前窗口 **7/10 已冻结**，不补写剩余槽位。中途恢复成功不等于最新保存后 restart；Gate 1 持续产量、Gate 2 全链自动供料/供电与覆盖、Gate 3 连续36000 ticks 均未证明。只有本文与[当前快照](../../current-status.md)的单一文档提交对应远端 CI 通过、且 root 明确 handoff 后才能开新窗。审核基线 main `e7a1e6ef0b6e972b9c48139e9331b9b3c6d596a2`；本次提交只含文档。installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104，228 runtime files + 2 native hashes 匹配），未部署。

## 前置阶段调用方边界（历史）

本次有两类本地调用方问题，不是游戏施工失败：一处递归 FunctionInfo 检查令流程在任何 Game 调用前停止（0 Game），随后固定为使用冻结 ScriptBlock；另一处错误要求 future recipe I/O budget 为空、且要求原生没有提供的 target ID 回显，导致 configure prepare falseguard，没有发出该配置 commit。原前两项 accepted 已核销，再由 fresh suffix 完成其余八项，无重放。18项离线 caller checks 覆盖原生 configure ordinal 315 正例。墙钟计量：prefix run 613.09 s（Craft action 终态 585.64 s、Collider 终态 20.99 s）；suffix 八项 149.71 s（委派入口至首个业务 prepare 4.15 s）；capture 26.81 s；root 独立 audit 26.61 s。各项为各自边界计时，不相加；provider usage unknown。上一材料阶段的 `DetailIds` 数组参数绑定与 `expectedStateHash` 字段修正见[材料储备阶段](warper-plan-material-reserve.md)。

该历史窗口 **10/10 冻结**。当前源码与运行版本边界见本文最新阶段及[当前快照](../../current-status.md)。
