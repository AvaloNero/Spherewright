# Fuel startup and owned resume

记录：2026-10-02（Asia/Singapore）。本文整理此前已完成的只读审计、安装 cohort 核对，以及当前新写窗的受保护 receipts；最近 Save 和四写独立审计均已核实通过，仍需后续有限采样及真实 restart 才能判断燃料 Gate。不复述凭据、计划 token、真实存档名、私有绝对路径或 raw body。

## 授权与结论边界

燃料验证按此唯一顺序推进：Gate 1 找到唯一直接根因、实施最小修复并取得稳定启动正例；Gate 2 建设 1210 链；Gate 3 此后才做至少 36000 ticks 的联合连续运行并证明两链均达到 ≥1/min。Gate 3 的持续门不是 Gate 1 / Gate 2 的准入条件。当前 Gate 1 尚未完成，短窗读数也不是 Gate 3 试验。旧写窗以九次已接受动作提前封存。前一阶段文档 commit `2c34bdcc68e7215f8a3b9980d4cae385a6385bc2` 已推送、远端同 SHA，CI `36894649554` 成功，且 root 已明确 handoff；当前新窗口 **4/10 accepted、提前封存**，本阶段限额为 7 个 accepted、已用 4，lifetime accepted 为 13。四项均有终态 receipt、独立审计 0 replay / unknown / in-flight。完整快照与 root 四写审计已 PASS；最新 fresh run `65858d773c7c4b589ecac6d8eba98168` 只含一个 600-tick 短窗，未证明 Gate 1 或 36000-tick 持续目标。下一写窗须等本阶段文档 commit/push、对应 CI 绿色及 root 明确交接后才能开启。本文不扩大既有授权范围。

## 启动与恢复

Steam 于 2026-10-01 23:23:44.497 +08 启动一次。启动后曾有一次只读 `REQUEST_TIMEOUT`；同一进程随后恢复正常响应。它不是第二次启动，也未触发写入重放。

受保护 run `resume-73e809688e314e42a157641fd211d774` 的原始记录：ordinal 1 是针对 owned primary 的 fresh resume 起点（planet 104、最低 Journal sequence 97、保存基线 tick 84232813）；ordinal 4 intent 的 RPC operation 为 `commit_resume_owned_game`；ordinal 5 commit 被接受且非幂等 replay；ordinal 27 为 `resume-owned-game / completed / terminal / succeeded`。会话随后为 R1 / planet 104 / DSP 0.10.35.29104 / healthy；收尾 session state 记录正常保存 tick 84232844。

Journal 从既有 J97 连续接入：收尾回执 ordinal 242 显示 durable-through 97、共 97 entries；root 将全部 97 entries 和 Journal ID 与既有基线 `629ad…` ordinal 28 比较，完全一致。该恢复使外部 accepted 从 8 增至 9；恢复自身已写出正常 primary save 84232844，所以不另发一个无意义的第十次 Save。

独立九写封窗 proof 是受保护 receipt `action-c955c6c77ba444f4a5c389985a8262cb` ordinal 1，SHA-256 `8770C8A93DD596718336C2B016851E8FE2EEFCEE7B08B2D0A8EE9617E2D2E61A`。审计读取 45 条 receipt、核实 9 个 unique accepted actions：9/9 successful terminal、0 replay、0 unknown、0 in-flight、0 unresolved commit；写窗 **9/10、提前冻结**。审计为 PASS，但通过范围是写窗核销与状态连续性，不是燃料 Gate 1。

冻结不追溯改写旧 accepted 计数。前述提交、CI 与 root handoff 已完成后，新窗口从 0/10 开始；现有受保护 receipts 记录 4/10 accepted，lifetime 13。accepted 数本身不替代 terminal/readback/Journal 审计。

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

较早纯离线字段/方法/路径错误的精确总数仍未知，不与本轮计数合并。本轮记录了 4 个 caller errors：无参 session assertion、错误 node 字段、对可选 `itemId` 的误用、错误的 `inspect_entity` 调用。无参 assertion 错误发生在任何 Game 请求前；随后改为先经 `Read-SpherewrightStageResult` 读取 `get_session_state`，再把返回的 state / SessionId / planet / version 传给断言，未改共享 helper。不要把原生几何/覆盖拒绝、计划覆盖不足拒绝或正常无人机返航等待计入这 4 次。此处是调用方问题，不是 native 或 identity failure。实际 ordinal 233 的唯一 native production method 是 `get_overseer_production`。离线重复读取或索引 receipt 不增加 native 调用数。此前较早记录中的 0.794/min 上界仍是历史窗口结论，不覆盖本次 600-tick 原始观测。

因此：九写审计已 PASS 并提前封窗；石矿源点替换、材料路径与接线已完成四写独立核验，但燃料端稳定启动正例与当前最小直接 blocker 是否排除仍未证明。Gate 2（建设 1210）尚未开始；Gate 3（至少 36000 ticks 联合连续运行、两链均 ≥1/min）尚未执行。旧 600-tick 短窗与本轮单个 production observation 都不能替代 Gate 3，也不能定出氢分配根因。当前新窗 4/10 accepted、lifetime 13；最近 Save 已成功，R8 / observed tick 84577456 / saved=true，lastOwnedSaveGameTick 84577452，J97 durable 且无 pending/error。sampler 汇总为 `samples=0 / requests=19`，但 ordinal 20 有完整 600-tick raw production；为什么该响应未被汇总为有效 sample 尚未定位，后续有限采样仍待完成。完整快照和 root 四写独立审计已 PASS。参见[当前快照](../../current-status.md)、[较早燃料持续性复核](../2026-10-01/fuel-continuous-readiness-check.md)及[Drive / save / fuel 边界](../2026-10-01/drive-four-save-and-fuel-graphite-boundary.md)。

## 后续现场的石矿副产品只读准备

只读 run `32fbe2e8e49449108dc2f49a96fc7a70` 共 17/17 成功 receipts、0 writes；ordinal 16 收尾为 tick 84466439、R1 / planet 104 / healthy，Journal 为 97/97。ordinal 13 的资源读数中，StoneGroup 15 有 13 个可见节点（ID 188–203 范围内），未开采合计 461521，均在玩家建造范围内且当前无矿机；item 2301 矿机库存 1 个，node 203 距旧矿机足迹约 2.4 m。

现场已有路径 `87 free input → 88…128 → 859 → 95`。ordinal 5 显示对象 95 的 item 1005 石矿库存为 0；ordinal 7 的对象 861（recipe 24，硫酸）缓冲为石矿 5（配方需要 8）、精炼油 12、水 8、硫酸 0；ordinal 8 的对象 869（recipe 31，石墨烯）缓冲为高能石墨 6、硫酸 0、石墨烯 0。完整 factory snapshot run `105ab304cd6f42ddbdfc25d0b3117848` ordinal 173 中，对象 3966 的 item 1109 石墨输出支路与对象 870 的静态连接仍在；这只是拓扑，不代表此次已恢复供货。当前对象 870 缓冲由 run `32fbe…` ordinal 9 读取：25 个石墨槽合计 2500、已满；5 个硫酸槽为空。

生产采样 run `d6fee3ebc7e444d1927b2f2faabfca46` 要求最多 3 窗、每窗 600 ticks、间隔 606 ticks，native request 上限 80 / 120 秒。第三窗前预算耗尽：terminal 为 `not_proven / request_budget_exhausted`，2 个 qualifying windows，80/80 requests，last tick 84440280，run-level coveredGameTicks 0、gameWrites 0；没有自动重启，也不重跑这份采样。

- 窗口 1，ticks 84439026–84439625：高能石墨（1109）产/耗 1/1 件，报告速率 6/6 件/min；精炼油（1114）2/1、12/6 件/min；氢（1120）4/2、24/12 件/min。重氢（1121）、氘核燃料棒（1802）及超级磁场环（1205）均为 0/0。
- 窗口 2，ticks 84439681–84440280：高能石墨 1/1、6/6 件/min；精炼油 0/1、0/6 件/min；氢 3/4、18/24 件/min。重氢、氘核燃料棒及超级磁场环仍均为 0/0。

上列先写窗口累计件数，再写该窗口 native 报告的产/耗速率；这是两个有效的不重叠短窗，不是三窗连续试验或稳定产率。计数按 unique native intent/response 核销；重复的内外层 receipt 不增加 native calls。sampler 的 80-request 预算不等于 Bridge 次数或 token 数；若把独立 opening J1 计入该 run 总数，则另加 1 次，合计 81 个 native calls。

### 新写窗内的三项施工动作

本窗目前 4/10 accepted（前九项旧窗仍冻结；lifetime 13）。第一项 run `3e99565b9a374fc9a91f4d48323d8acd` / action `984c4971…`：terminal ordinal 31、实体 ordinal 32、player ordinal 3/33。矿机 object 86 仅覆盖 node 203，消耗 item 2301 一台，network 3 ratio 1；root 已独立核实这项，源码未变。

第二项 run `6c67a5d2df3744ed94197c148ed6a86e` / action `161d1000…`：9 段新 belt（5316–5324）实际连接 object 86 → 5324 → … → 5316；player inventory/stacks 与对应 NormalActionResult.itemDeltas 记录 item 2001 传送带库存由 260 降至 251。root 已独立核实原回执终态及 9 段逐对象双向边。第三项 run `2a3682c4e0a14f0aa9fa8a503eb60e97` / action `0a359349…`：sorter 5325 设 filter 1005，belt 5316 slot 4 → sorter slot 1，sorter slot 0 → belt 87 slot 4；player inventory/stacks 与对应 NormalActionResult.itemDeltas 记录 item 2011 分拣器库存由 2 降至 1。root 已独立核实该终态、filter 与双端持有；87→88 的旧路径保留。最终有向路为 object 86 → 9 段新 belt → sorter 5325 → belt 87 → 原 belt 88。

几何/接口边界已纠正：先前 empty-cover 拒绝是因为 source 86 为 device、不属于该操作支持的 empty-cover source 子集；destination 87 原有 slot 0 → 88 正常输出并非拒绝原因，且旧路径未改。矿机没有 inserter pose，不能直接接 sorter；最终用 source 86 → 9 段新 belt → sorter 5325 → belt 87，不移除旧成功实体。前置 readiness run `686bb85…` ordinal 3 是在此前施工成功后因 3 架无人机返航、0 pending 而拒绝；经 60 秒有界只读等待后重新 fresh 检查，无 belt 重放。上述几何与返航情况不是 caller errors。

### 本轮 production 与 Save

run `c8913aa1b7fb4981a0dccc15071237ab` 的 Save 原索引唯一 accepted 为 `b16f243f-a153-4de3-840d-97001815ccfe`：commit ordinal 27，terminal ordinal 28 成功，save tick 84577452。ordinal 29 为 R8 / observe tick 84577456 / saved=true / lastOwnedSaveGameTick 84577452 / healthy / owned / restartResumeAvailable=true；ordinal 30 Journal durable 97、pending=false、error=null。此前 R7 的 `saved=false` 是更早状态，不是 Save 失败；不重发 Save。

该 run 的 native production ordinal 20 提供完整 600-tick 窗口（84576828–84577427）。以下全部为窗口累计件数 P/C，不是每分钟速率：

| item | 窗口累计产 / 耗 |
|---|---:|
| 石矿 1005 | 6 / 4 |
| 硫酸 1116 | 4 / 0 |
| 石墨烯 1123 | 2 / 3 |
| 高能石墨 1109 | 3 / 3 |
| 氢 1120 | 12 / 8 |
| 重氢 1121 | 0 / 20 |
| 氘核燃料棒 1802 | 0 / 0 |

这是一份可独立保留的短窗原始 production 观测，尚不能证明持续产率或 Gate 1。sampler 汇总为 `samples=0 / requests=19`，但 ordinal 20 的 native production receipt 完整；为什么该响应未被汇总为有效 sample 尚未定位，不能把该汇总称为实验通过，也不能说成 native production 缺失。离线 `test-production-sampling` 检查为 27/27 PASS；protected-serialization 另有 1 条 fixture PASS，不是 27 条序列化测试。其 receipt 请求量核对为 9 次 session poll、8 次 entity 查询、1 次 power、1 次 production（共 19 次 native requests）；provider usage unknown，不换算或声称 token 数。miner 86 当前 working；object 3403 的 working 属启动批次，不代表燃料链完成。root 已核 Save receipt；后续仍需有限采样。

root 批准的全阶段限额是至多 7 个 accepted（矿机、belt、sorter、Save 合计），曾以 6 / 9+ 节点覆盖作为初始效率设计；该设计不是准入条件。依据已证明的 native node 203 覆盖正例，当前改为 exact nodes [203]、item 2301 ×1 的受限预算，不代表降低燃料安全目标 ≥1/min 或修改 whitelist。本窗已 accepted 1 个矿机动作、9 段 belt、1 个 sorter 和 1 个成功 Save，共用 4/7；不得移除旧成功实体。node 203 单点约有 4.3 万石矿；recipe 24 / 31 / 40 / 41 的物料平衡给出达到 fuel 1/min 所需石矿最低约 13⅓/min，这只是预算估算，不是实测产率或 Gate 证明。

## 四写独立审计与 fresh 现场读数

root 对本阶段四个 accepted writes 独立审计 PASS，受保护 proof 为 `action-873068b66a5b4c3aa426347771f66cd6-0001-stone-fuel-four-independent-audit.json`，SHA-256 `AC2DBC41C5E7EC36D023B5BA1A55F284126220DB250CB8F252E6764AC60DD238`。审计核实 0 replay / unknown / in-flight。

完整快照 run `1bf0a4a449f24781970df6311864e4ac`：ordinal 2–55 共 54 页、5325 个对象，capture tick 84601331；ordinal 59 prebuild=0，player 56、Journal 57、power 58、session 71。与 5314-object 基线比较，旧实体的 15 个静态字段及 station configuration 均不变；只新增 object 86 和 belt/sorter 5316–5325，object 87 仅多出指向 5325 的反向边。10386 条边均唯一互反。背包唯一计数变化为 item 2301 −1、2001 −9、2011 −1，其余 count/inc 无未解释变化。network 3 只多两个 consumers，容量、nodes、发电机均保留，ratio=1。

ordinal 71 的 session 为 R8 / observe tick 84601862、lastOwnedSaveGameTick 84577452、healthy / owned；Journal 97。之后 fresh production run `65858d773c7c4b589ecac6d8eba98168` ordinal 2 提供窗口 84602650–84603249（600 ticks）。以下全是累计件数 P/C，不是速率：

| 产品 | 窗口累计产 / 耗 |
|---|---:|
| 石矿（1005） | 6 / 3 |
| 高能石墨（1109） | 5 / 3 |
| 硫酸（1116） | 0 / 0 |
| 氢（1120） | 19 / 21 |
| 重氢（1121） | 5 / 20 |
| 石墨烯（1123） | 0 / 0 |
| 氘核燃料棒（1802） | 0 / 0 |

该单窗不能证明持续速率。相邻只读缓存为 object 863 空、870 石墨 2500 / 硫酸 0、869 硫酸 0；这些读数不足以判断硫酸链的直接根因。保存后没有真实 restart；燃料端稳定启动正例仍未证明，restart 不作为 Gate 1 准入条件，36000-tick sustained 也尚未完成。当前 4/10 窗口提前冻结、lifetime accepted 13，不归零；新窗口必须等本阶段准确 SHA 推送、对应 CI 绿色及 root 明确 handoff 后才能重开。
