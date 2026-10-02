# Warper 自动供料与建造准备

记录：2026-10-02（Asia/Singapore）。本文保留前置入口/材料事实，并记录当前有限接口施工封窗；后者不是完整自动供料、整链产能或重启验收。

## 前置入口与材料窗口（历史封存）

此前窗口从 0/10、lifetime accepted 23 开始，10/10 冻结、lifetime 33。runs `0efb2a182699476396adf21bb0c4c961`、`d78ba66493824f3985229e0c3c1ccd0a`、`beed2d28735243ba9bc5e8d5dc2516b4`、`b72af1dce67d43cfb3630c58e337370c` 合计十个唯一 resolved/completed action，0 replay、unknown、unresolved 或 in-flight。`b72af1...` 的六项包含四次 Transfer（两种物料各取、放一次）：item 1204×20 从 827 取出并放入 884，item 1104×20 从 26 取出并放入 884。884 从 default 改为 lock-occupied，两个输入格分别配置对应物料，其余格为 0，原三路输出保留。normal save tick 84852841 / R37，durable J97。

Root 独立审计 PASS：proof `action-22ffbb33c912411098ccdb40ae3df7f0-0001-warper-inlet-independent-audit.json`，SHA-256 `5B1850ABE88C0AEDDE35732BB1BC9E2DD04531200D4A0A28228C67F06F6C5363`（29.77 s，0 Game calls）。capture `cb17c8eb06bb4ae0b68494356042cc15` 为 54 页、5325 实体；全图 10386 条边均唯一互逆，无实体新增或删除。物品 count/inc 与原生 action deltas 守恒：Fe +292、Mag −4、Cu −2、Tesla +4，其余不变；power topology/capacity 不变、consumer ratio=1，prebuild=0；J97 身份和97条记录连续。

该窗口的设计上下文仍有限：旧北侧图 `c75e8d9b6bf1465ba42d1138cdc3abbd` 中 A3 为 `NeedGround`、B 已取消；power-context 不是完整覆盖证明。石矿 node 203 已由 miner 86 覆盖。只读 site 检查 `2332beaae1d14310ab68046d8efe15c8` 共17次：Lens/Warp `NativeOk`，Diamond 西侧及 object 2310 + Tower A 为 `NeedGround`；Tower/Storage 仅 prepare-only，未建实体。旧列距12布局 snap 后两 ASM bounding overlap，不能据旧布局或 prepare-only 结果宣称全链可执行。

## 当前有限施工封窗

外部新窗从 0/10、lifetime 33 开始。stage runs `02944d6232df44fe89801e6a27c5bb68`（先完成两项）与 `7d53ff4826d64af4bdc3224948441d48`（fresh suffix 八项）合计 10 个唯一 completed action；0 replay、unknown、unresolved 或 in-flight，lifetime accepted 43。首 run 完成 recipe 84 Craft×120（belt +360、Fe −360）及 collider 建造（item 2310 / object 5326）；suffix 配置 collider 5326 recipe 104，建造 Tesla 5327/5328、assembler 5329 并配置 recipe 78、Tesla 5330、stock 5331，最后 normal save action `871d078d-6293-4bf1-9990-cceaa2de75ad` 于 tick 84915771 完成。

Root 独立全图审计 PASS（26.61 s，0 Game）：proof `action-a3c3da97a47843b295eb202c04397030-0001-warper-construction-independent-audit.json`，SHA-256 `127F7D65DF39A87064BA02F21284AE6D31E2E05837D49EFC8AA50B36F16284FD`。capture `556086d58ba24144860466df5dad3995` 为54页/5331实体、0 prebuild，capture tick 84916675、closing observed tick 84917231；save/readback 为 tick 84915771 / R56 / durable J97。原5325实体静态配置及10386条互逆边全部保持；新增6实体无物流连接引用，其中两台配方机器正常接入 net3。net3 nodes 206→209、consumers 512→514，发电机与容量不变、ratio=1。玩家物料净额为 Fe/item 1101 −360、belt/item 2001 +360、collider/item 2310 −1、assembler/item 2303 −1、Tesla/item 2201 −3、stock/item 2101 −1；其余 inventory count/inc 不变。J97 identity 与97条 entries 逐字一致。

这只证明一次有限的未来接口施工 qualification。**Gate 2 整体仍未完成**：完整 executable 方案、自动供料、整链供给/覆盖、持续产量、实际保存后 restart 及 Gate 3 连续 36000 ticks 均未证明；Gate 1 虽已有 native 启动正例，持续 ≥1/min 仍未证明。此前的 prepare-only 或 NativeOk 只说明对应查询/位置边界，不代表生产链已运行。

## 调用方边界与冻结

本次有两类本地调用方问题，不是游戏施工失败：一处递归 FunctionInfo 检查令流程在任何 Game 调用前停止（0 Game），随后固定为使用冻结 ScriptBlock；另一处错误要求 future recipe I/O budget 为空、且要求原生没有提供的 target ID 回显，导致 configure prepare falseguard，没有发出该配置 commit。原前两项 accepted 已核销，再由 fresh suffix 完成其余八项，无重放。18项离线 caller checks 覆盖原生 configure ordinal 315 正例。墙钟计量：prefix run 613.09 s（Craft action 终态 585.64 s、Collider 终态 20.99 s）；suffix 八项 149.71 s（委派入口至首个业务 prepare 4.15 s）；capture 26.81 s；root 独立 audit 26.61 s。各项为各自边界计时，不相加；provider usage unknown。上一材料阶段的 `DetailIds` 数组参数绑定与 `expectedStateHash` 字段修正见[材料储备阶段](warper-plan-material-reserve.md)。

当前本窗 **10/10 冻结**；只有本文件与[当前快照](../../current-status.md)的单一文档提交远端 CI 通过、且 root 明确 handoff 后，才可开启新窗。当前源码 pin `5f58779016b4c63471ba2b8347b7c366dbef9d83`；installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）。未部署。
