# Fuel startup and owned resume

记录：2026-10-02（Asia/Singapore）。原始事件集中在 2026-10-01 深夜。本文只整理本地受保护 receipts、已完成的独立只读审计和安装 cohort 核对；不复述凭据、计划 token、真实存档名、私有绝对路径或 raw body。

## 授权与结论边界

当前唯一授权顺序为：Gate 1 找到唯一直接根因、实施最小修复并取得稳定启动正例；Gate 2 建设 1210 链；Gate 3 此后才做至少 36000 ticks 的联合连续运行并证明两链均达到 ≥1/min。Gate 3 的持续门不是 Gate 1 / Gate 2 的准入条件。当前 Gate 1 尚未完成，短窗读数也不是 Gate 3 试验。写窗以九次已接受动作提前封存；本文不授权后续写入。

## 启动与恢复

Steam 于 2026-10-01 23:23:44.497 +08 启动一次。启动后曾有一次只读 `REQUEST_TIMEOUT`；同一进程随后恢复正常响应。它不是第二次启动，也未触发写入重放。

受保护 run `resume-73e809688e314e42a157641fd211d774` 的原始记录：ordinal 1 是针对 owned primary 的 fresh resume 起点（planet 104、最低 Journal sequence 97、保存基线 tick 84232813）；ordinal 4 intent 的 RPC operation 为 `commit_resume_owned_game`；ordinal 5 commit 被接受且非幂等 replay；ordinal 27 为 `resume-owned-game / completed / terminal / succeeded`。会话随后为 R1 / planet 104 / DSP 0.10.35.29104 / healthy；收尾 session state 记录正常保存 tick 84232844。

Journal 从既有 J97 连续接入：收尾回执 ordinal 242 显示 durable-through 97、共 97 entries；root 将全部 97 entries 和 Journal ID 与既有基线 `629ad…` ordinal 28 比较，完全一致。该恢复使外部 accepted 从 8 增至 9；恢复自身已写出正常 primary save 84232844，所以不另发一个无意义的第十次 Save。

独立九写封窗 proof 是受保护 receipt `action-c955c6c77ba444f4a5c389985a8262cb` ordinal 1，SHA-256 `8770C8A93DD596718336C2B016851E8FE2EEFCEE7B08B2D0A8EE9617E2D2E61A`。审计读取 45 条 receipt、核实 9 个 unique accepted actions：9/9 successful terminal、0 replay、0 unknown、0 in-flight、0 unresolved commit；写窗 **9/10、提前冻结**。审计为 PASS，但通过范围是写窗核销与状态连续性，不是燃料 Gate 1。

冻结后不自动重置 accepted 或打开下一写窗。下一写窗须等待本次文档单一目的 commit/push、对应 CI 绿色以及 root 明确 handoff；本文不是 handoff。

## 安装 cohort 核对

installed cohort `6bf35b7…` 为 0.4.0 cohort。Plugin 4 + MCP 224 共 228 文件均与 manifest 哈希匹配；native 引用 2/2 对齐；Plugin/MCP 源码无变化。64 tools / 1 resource 是历史索引，不能称为这次 fresh handshake。此次没有重装、重部署或新增插件/MCP 调用面。相关历史部署边界见[固定 AutoSave0 恢复记录](../2026-10-01/active-fixed-autosave-recovery.md)。

## 新的只读现场

只读 run `105ab304cd6f42ddbdfc25d0b3117848`：80 次 Bridge read、33.963 秒、0 write。主要原始 receipt 锚点为 ordinal 173（完整 factory snapshot）、176（prebuild/实体查询）、233（native overseer production）、236（power summary）、239（closing session state）及 242（Journal）。

- ordinal 173：54 页、5314 实体、capture tick 84306887。与 5315 实体基线的完整比较中，15 个静态字段及 station configuration 对其余 5314 实体无变化；唯一对象移除是 86，唯一对应的 reverse edge 移除为 87；10364 条保留边均有唯一 reciprocal 对端。无新增实体。ordinal 176 prebuild 结果为 0。
- 唯一其他静态差异为矿机 106 的 `resourceNodes` 从 `[312]` 变成 `[]`；ordinal 233 的 native infrastructure finding 同时报 `vein_exhausted`，remaining resource 为 0，故属矿脉耗尽，不是未解释的拓扑/燃料变化。
- 独立核对的 17 行玩家库存中 count/inc 均与既有基线相同；无步行、3 架无人机均 idle、无 pending/无 crafting。Journal 仍为相同 ID 与全部 97 entries。该快照是限定现场比较，不扩大成其他星球/全厂普遍结论。

### 600-tick 生产短窗

ordinal 233 的实际 native method 是 `get_overseer_production`。窗口为 tick 84307953–84308552（600 ticks）；下表把累计件数与 native 以该窗报告的每分钟速率分开：

| 产品 | 窗口累计产 / 耗 | 窗口报告产 / 耗速率 |
|---|---:|---:|
| 高能石墨（1109） | 1 / 1 件 | 6 / 6 件/min |
| 精炼油（1114） | 2 / 1 件 | 12 / 6 件/min |
| 氢（1120） | 4 / 12 件 | 24 / 72 件/min |
| 重氢（1121） | 0 / 0 件 | 0 / 0 件/min |
| 氘核燃料棒（1802） | 0 / 0 件 | 0 / 0 件/min |

这是 600 游戏 ticks 的短窗，不是持续产率证明；特别是 item 1802 在此窗未记录产/耗，不足以解释氢的全程分配，也不能据此猜定 `5187` 与 belt `3365` 的竞争为根因。对象 3074 的 storage `valid[]` 为空，H/D 为 0；对象 3079 的 `power-generation-current-tick` 为 16122 J/t。后者即使 `isWorking=false` 也不能解释为停止发电。

### 供电与收尾

ordinal 236（capture tick 84308565）记录 network 3 的 required/served 均为 303250 J/t、capacity 1894000 J/t、consumer ratio 1.0；network 4 为 1800/1800 J/t、capacity 10000 J/t、ratio 1.0。全网合计 required/served 305050 J/t、capacity 1904000 J/t。ordinal 239 收尾 tick 84308579，world healthy、R1、planet 104，最近正常保存仍为 84232844；ordinal 242 的 J97 durable-through 97。该点样证明这些读数时供电满足需求，不是持续燃料 Gate 的替代品。

## 计量方法与未完成项

root 在纯离线解析中出现字段/方法/路径错误；精确总数未知，均未发出 Game/Bridge 请求。实际 ordinal 233 的唯一 native production method 是 `get_overseer_production`。离线重复读取或索引 receipt 不增加 native 调用数。此前较早记录中的 0.794/min 上界仍是历史窗口结论，不覆盖本次 600-tick 原始观测。

因此：九写审计已 PASS 并提前封窗；Gate 1 仍未完成，因为唯一直接根因、最小修复和稳定启动正例尚未证明。Gate 2（建设 1210）尚未开始；Gate 3（至少 36000 ticks 联合连续运行、两链均 ≥1/min）尚未执行。item 1802 在该 600-tick 短窗的 0/0 读数不能替代 Gate 3，也不能定出氢分配根因。下一写窗仍需文档 commit/push、CI 绿色和 root 明确 handoff。本阶段不声称 fresh MCP handshake、部署、持续产出或最终版本验收。参见[当前快照](../../current-status.md)、[较早燃料持续性复核](../2026-10-01/fuel-continuous-readiness-check.md)及[Drive / save / fuel 边界](../2026-10-01/drive-four-save-and-fuel-graphite-boundary.md)。
