# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式摘要，不是机器状态源；session、accepted 计数和 action 终态以最新受保护回执为准。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 源码 / 安装 | 本阶段 pinned source `8c52d5424ae25009853c40de5581df52b7a0a459`；installed runtime `6bf35b7b81a2e50c8e9f42feebbc1f15552096de` | DSP 0.10.35.29104；既有安装228 files / 64 tools / 1 resource。root核对两版本 `src` 与 `tests` 无差异；未重新部署或重核安装哈希。 |
| 游戏会话 | owned-world-001 / planet 104 / R7，最新 session 观察 tick 84484806，healthy | 最新正常保存 tick 84232813；最新 Journal J97 durable，与保存后逐条相同，不是同tick采集。额外player观察84484827；真实 restart/resume 尚未验证。 |
| 科技 | 2104 于 tick 83027891 解锁；2904 于 tick 84027632 解锁；均 4/4 完成、队列空 | 两项进度均已由上述正常保存覆盖；不代表新燃料链或当前 Foundry 草案已施工。 |
| 外部写窗口 | accepted **8/10 OPEN** | 本阶段新增0；17次成功只读，超过15次批准预算两次。writer已停止，无 unknown、无在途、无未核销写入；不替代 fresh 全厂 census。 |

## 当前唯一阶段：紫糖源—路—端

`1402 → 4743 → 6004 → Lab84 → 2104` **未完成，停止于需求门**。Fresh runtime确认1402为粒子宽带、1303为处理器；4743有处理器6但粒子宽带0。源2255缺碳纳米管，向上追到861硫酸配方所需原石5/8；95原石0、石材2198不能代替。已有缓存定向路线连通，本批不再增加带、分拣器、电塔、仓储或旁路。

2104已满300000/300000并解锁，科研队列空，Lab84无目标且停机；3051仍有1092紫糖，Lab84紫色缓存36000科研点=10件。600-tick窗口84484192–84484791中1005、1402、6004均0产/0耗，相关网络采样满供电。指定科研消费门不存在，因此未开始施工或A/B/C生产实验，不擅自改选科技。权威原回执、源—路—端表与计量见[本阶段证据](evidence/2026-10-01/purple-demand-gate-and-stone-shortfall.md)。

## 燃料与石墨边界

唯一权威燃料记录为[燃料持续性就绪复核](evidence/2026-10-01/fuel-continuous-readiness-check.md)：预声明至少 1 棒/分钟未通过；重叠采样给出的速率上界为 0.794 棒/分钟，不能当作实际速率。氘供应/分配的物理修复未定位，不降低持续门。

本阶段只读诊断观察了石墨与氘/重氢设备的瞬时状态，不能据短窗宣布全厂停机或唯一长周期因果。复用的 5315 对象场景是缓存快照，不是 fresh 全厂预检；现有图上存在通向下游的 reciprocal 路径，但 5187 与其他石墨负载共用 belt 3365 是否造成竞争仍是假说。详见本阶段[Drive、存档与燃料/石墨边界](evidence/2026-10-01/drive-four-save-and-fuel-graphite-boundary.md)。

## Foundry 只读草案

Foundry 对 1210、目标 1/min 的深度 3 计划返回 `executable=false`：金刚石 4/min → 引力透镜 1/min → 空间翘曲器 1/min。依赖奇异物质 1127 的自动外供，尚未证明；物流、场地、库存预算和供电也未验证。建筑物品 2302×1、2303×2 不是实体 ID 或整链预算。该读取不是 native preflight、施工或持续产出证明，也不授权开始建造。

## 回归与计量边界

最近源码回归为2416个 Core/Contracts/MCP tests 全通过、完整 Release 构建0 warnings / 0 errors；实际pwsh及私有SDK8.0.424、先前启动失败与日志索引见[上一事件](evidence/2026-10-01/drive-four-save-and-fuel-graphite-boundary.md)。当前纯证据批未改代码，不重复无关全套本地构建。

上一保存/燃料阶段的47次native calls及52.337分钟诊断→save间隔见其事件记录，不混入本批17次只读。当前批原响应跨度4.154秒；两次超预算读取、一次少报调用及root三次离线解析错误均已按原文件核销，未追加Game调用或重放。完整委派、文档/Git/CI和provider用量未知，不宣称整体提速。无新 Plugin/MCP surface、安装或部署变化。

## 未完成目标与安全边界

石墨/氘燃料仍未通过持续门；Foundry 的三级供应计划仍是不可执行只读草案，自动 Matter 外供及中途恢复未验证。紫糖 1402→4743→6004→Lab84 仍未端到端完成；2104 已由历史库存解锁，不得据此宣称紫糖路线通过或擅自改选目标。未证明持续燃料、Foundry 实体建设、真实重启或最终双包验收。

只对精确核验的 owned identity 执行有界动作，遵守 fresh prepare、计划校验、唯一 commit、同 action terminal/readback 与 durable Journal。accepted 不因 revision、tick 或 Journal 变化清零；摘要异常或断线不授权重放。遇到 unknown、quarantine、身份/版本漂移或结果无法核销时冻结新写并交 root。凭据、token、真实存档名、绝对私有路径和 raw body 不入库。
