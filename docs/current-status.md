# Spherewright 当前快照

更新：2026-10-02（Asia/Singapore）。本页为覆盖式快照；当前输出端资格窗口的唯一阶段证据见[Warper 自动供料与建造准备](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前运行与写窗

- 当前外部窗口 **10/10 冻结**，lifetime accepted 60；10个唯一写动作均成功完成，0 replay、unknown 或 in-flight。root 独立全图审计 PASS。剩余槽位不补写；下一窗口须等待本阶段文档提交对应 CI 通过及 root 明确 handoff。
- normal save 为 tick `85002321` / R29 / durable J97，覆盖本窗全部10个写动作。其后 factory capture tick `85002688`、closing observed `85002847`；capture 是保存后的观察，不代表其后动态缓冲也已持久化。捕获时1架返航无人机非 idle、pendingBuildTargets=0、pendingRepairTargets=0，不将其误判为未决写。
- 窗口由 main 基线 `b7658e2` 的远端状态与 CI 绿、并经 root handoff 后开启。完整捕获 `6477dc98015f481a8e3e4f2c3925a07e` 为54页/5365实体/14项 detail/0 prebuild；独立审计 proof SHA-256：`7B5444BDE3EA5FD201DF202703CD955F6275D73E726F6CF74EE624BCD7FC7FA0`（0 Game calls）。
- 原5331实体静态内容保持，例外为 object 884 的 filter grid 配置和 object 5329/5331 两个新 sorter 端点；原10386条互逆边保留，新增34个实体、64条有向互逆边，总10450条均互逆。net3 增1 node、增4 consumers，容量/发电保持、consumer ratio=1。J97身份与既有历史连续。
- 29条 item 2001 belt 将 object 5329 与 5331 接通；sorter 5364 filter=item 1210、从 `5329.slot7` 接首带，sorter 5365 filter=item 1210、从末带接 `5331.slot11`。这是接线/过滤配置，不证明货物流动。玩家净消耗为 belt 29、sorter 2、Tesla 1、assembler 1、炉 1（belt 611→582）；其他 inventory count/inc 不变。
- installed cohort/source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP `0.10.35.29104`；228 runtime files 与2个 native hashes 匹配）未变；本阶段未部署。

## 目标与边界

- 本次仅完成有限输出端资格接线与保存，不等于完整自动供料或整链可执行。Gate 1 稳定启动正例已存在，但持续 ≥1/min 未证明；Gate 2 自动供料/整链供给未完成；最新 save 后 restart、Gate 3 连续 36000 ticks、final pack 均未证明。
- 唯一下一阶段 blocker 是完成自动供料。此前15个只读/prepare调用中，三次路径 prepare 因已有 belt overlap 被拒、没有 commit；不得将其计作写入或盲目重试。
- Gate 2 完整方案未 executable 前仅只读/prepare-only；启动正例 + normal save + protected restart + 恢复后输出是当前结束门，联合长窗、远征与 final pack 延后。
