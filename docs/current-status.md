# Spherewright 当前快照

更新：2026-10-02（Asia/Singapore）。本文件是覆盖式摘要，不是机器状态源；状态以原始受保护回执和独立审计为准。当前唯一授权按序为 Gate 1（定位唯一直接根因、实施最小修复并取得稳定启动正例）、Gate 2（建设 1210 链）、Gate 3（之后才联合连续运行至少 36000 ticks 且两链均 ≥1/min）。Gate 3 的持续门不是 Gate 1 / Gate 2 的准入条件。

## 会话与写窗

- Steam 于 2026-10-01 23:23:44.497 +08 启动一次；初期出现一次只读 `REQUEST_TIMEOUT`，同一进程随后正常恢复。没有第二次启动或因超时重放写入。
- 受保护恢复 run `resume-73e809688e314e42a157641fd211d774`：ordinal 27 为 `resume-owned-game / completed / terminal / succeeded`。从原 primary 正常保存 tick 84232813 恢复后，session 为 R1、planet 104、DSP 0.10.35.29104、healthy；随后 native normal save 为 tick 84232844。Journal 97/97 durable，全部 entries 与既有 J97 基线一致。
- 九写独立审计 **PASS**：9 accepted、9 successful terminal、0 replay、0 unknown、0 in-flight、0 unresolved commit。窗口按 9/10 **提前冻结**；恢复已正常保存，不为凑第十次而另发 Save。此审计通过不等于燃料门通过。
- 阶段源码 pin 为 `07f246cd511d51c26bbc53c6543530491235832a`；installed runtime/cohort source 为 `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）。Plugin 4 + MCP 224 共 228 文件与 manifest 哈希匹配，native 引用 2/2 一致；Plugin/MCP 源码未变。64 tools / 1 resource 是历史索引，不是本次新握手。本阶段未重新安装或部署。

## 当前燃料读数

只读 run `105ab304cd6f42ddbdfc25d0b3117848` 共 80 次 Bridge 读取、33.963 秒、0 写。完整快照为 54 页 / 5314 实体（tick 84306887）；prebuild 返回 0，收尾 tick 84308579，normal save 84232844，Journal 97/97。完整审计和来源见[本阶段证据](evidence/2026-10-02/fuel-startup-and-owned-resume.md)。

在 tick 84307953–84308552 的 600-tick 窗口，native `get_overseer_production` 记录：石墨 1/1 件（6/6 件/min），精炼油 2/1（12/6 件/min），氢 4/12（24/72 件/min），重氢 0/0，item 1802 氘核燃料棒 0/0。前列是该窗口的件数（产/耗），括号为该短窗报告的速率；不能解释为持续生产，也不能由满供电推出燃料达标。Gate 1 仍未通过：唯一直接根因、最小修复及稳定启动正例尚未证明；该短窗不是 Gate 3 测试。

同一采样的 network 3 为 303250/303250 J/t、capacity 1894000 J/t、ratio 1；network 4 为 1800/1800 J/t、capacity 10000 J/t、ratio 1。该点样只能说明采样时负载获供，不证明燃料链或长期稳定。氢分配因果仍未定；对象 5187 / belt 3365 的竞争仍是假说，不能写成根因。

## 仍未证明

原生只读诊断确认矿机 106 的 `resourceNodes` 从 `[312]` 变为 `[]`，同窗 native finding 为矿脉耗尽、剩余 0；这是相应静态差异的解释，不是燃料修复。对象 3074 的 storage `valid[]` 与 H/D 为 0；对象 3079 报告 `power-generation-current-tick=16122 J/t`，即使 `isWorking=false` 也不等于零发电。本地字段/方法/路径解析曾出错，精确总数未知，均未产生 Game/Bridge 调用；计量只采用 ordinal 233 的真实 `get_overseer_production` 回执，重复读取本地回执不计作额外 native 调用。

紫糖 `1402 → 4743 → 6004 → Lab84` 仍停在原需求门，见[紫糖阶段证据](evidence/2026-10-01/purple-demand-gate-and-stone-shortfall.md)；本阶段没有施工或改选目标。Gate 2 的 1210 建设尚未开始；Gate 3 的 ≥36000-tick 联合运行（两链均 ≥1/min）尚未执行。氢分配根因与 Gate 1 所需最小修复 / 稳定启动正例仍未证明。9/10 窗口冻结后，不自动重置 accepted 或开启新窗；须待本次文档单一目的 commit/push、对应 CI 绿色且 root 明确 handoff。较早燃料短窗仍按历史记录保留；本快照不把历史观测覆盖成当前结论。凭据、计划 token、真实存档名、绝对私有路径和 raw body 不入库。
