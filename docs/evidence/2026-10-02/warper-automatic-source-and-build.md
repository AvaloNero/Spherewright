# Warper automatic source and build preparation

记录：2026-10-02（Asia/Singapore）。本阶段已完成一次十写封窗独立审计；此处记录的施工边界仍有限，不能据此宣称完整整链建成、自动供料、持续产量或 restart 验收通过。

## 封窗结果

窗口以 0/10、lifetime accepted 23 开始，现为 **10/10 冻结**、lifetime 33。十项均核销为唯一 resolved/completed，0 replay、unknown、unresolved 或 in-flight。此前四项为 run `0efb2a182699476396adf21bb0c4c961` 的两项 Craft/Transfer、`d78ba66493824f3985229e0c3c1ccd0a` 与 `beed2d28735243ba9bc5e8d5dc2516b4` 的两项 Move；run `b72af1dce67d43cfb3630c58e337370c` 再完成六项，其中四次 Transfer（两种物料各取、放一次）将 item 1204×20 从 827 取出并放入 884、item 1104×20 从 26 取出并放入 884。884 从 default 改为 lock-occupied，两个输入格分别配置对应物料，其余格为 0，原三路输出保留。该窗以 normal save tick 84852841 / R37 收尾，durable Journal J97。

root 独立审计 PASS：proof `action-22ffbb33c912411098ccdb40ae3df7f0-0001-warper-inlet-independent-audit.json`，SHA-256 `5B1850ABE88C0AEDDE35732BB1BC9E2DD04531200D4A0A28228C67F06F6C5363`（29.77 s，0 Game calls）。完整 capture `cb17c8eb06bb4ae0b68494356042cc15` 为 54 页、5325 实体，capture tick 84853236、closing observed tick 84853679。全图 10386 条边均唯一互逆，无实体新增或删除；唯一静态配置变化是 884 的两个输入格。物品 count/inc 与原生 action deltas 守恒一致：Fe +292、Mag −4、Cu −2、Tesla +4，其余不变；power topology/capacity 不变、所有 consumer ratio=1，prebuild=0；durable J97 的身份与97条记录连续。

## 施工准备与未证明项

当前仍是入口/材料准备，不是完整建设。旧北侧图 `c75e8d9b6bf1465ba42d1138cdc3abbd` 中 A3 为 `NeedGround`、B 已取消；power-context 仅是局部上下文，不能代表完整覆盖或全厂通过。石矿 node 203 已由既有 miner 86 覆盖。SiteLens/Warp `2332beaae1d14310ab68046d8efe15c8` 为 17 次只读检查：Lens/Warp 为 `NativeOk`，Diamond 西侧及 object 2310 + Tower A 为 `NeedGround`；Tower/Storage 例子仅到 prepare-only，未建实体。旧列距12布局 snap 后两 ASM bounding overlap，须重排；不得把旧布局或 prepare-only 结果当作可执行全链。

root 正基于已证明的东侧地面重排三级模块及整条供给链。尚无完整 executable 方案、自动供料、全厂供电/覆盖、持续 ≥1/min、保存后真实 restart 或 Gate 3 连续 36000-tick 证明。Gate 1 的 native 启动正例已通过，但上述持续与 restart 事实仍未证明。

## 调用方诊断与写窗边界

历史 `DetailIds` 数组参数绑定与逐 ID 只读确认见[材料储备阶段](warper-plan-material-reserve.md)。另有本地调用方对 prepare 使用错误 `stateHash` 字段，经修正为真实 `expectedStateHash`；基于原回执的离线正、反例 fixtures 17/17 通过，未形成 commit intent 或 action。

本窗口保持冻结，须待本次文档提交的远端 CI 成功且 root 明确 handoff 后才可开启新窗口。本阶段代码基线 `35eb81b968499c77b7b35c3d4fcf3c5b5fd3f90c`；installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）。参见[当前快照](../../current-status.md)。
