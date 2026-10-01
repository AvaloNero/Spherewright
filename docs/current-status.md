# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式摘要，不是机器状态源；session、accepted 计数和 action 终态以最新受保护回执为准。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 源码 / 安装 | pinned source `b3264af9812552c359c7f6331d35dcca283d9b7e`；installed runtime `6bf35b7b81a2e50c8e9f42feebbc1f15552096de` | DSP 0.10.35.29104；228 files / 64 tools / 1 resource。root核对两版本 `src` 与 `tests` 无差异；未重新部署。 |
| 游戏会话 | owned-world-001 / planet 104 / R7，最新 session 观察 tick 84244696，healthy | 最新正常保存 tick 84232813；最近 Journal 在保存后读回 J97 durable，不是 84244696 同 tick 采集。真实 restart/resume 尚未验证。 |
| 科技 | 2104 于 tick 83027891 解锁；2904 于 tick 84027632 解锁；均 4/4 完成、队列空 | 两项进度均已由上述正常保存覆盖；不代表新燃料链或当前 Foundry 草案已施工。 |
| 外部写窗口 | accepted **8/10 OPEN** | 本阶段唯一新增 accepted 为正常保存；无 unknown、无在途写、无本阶段施工写入。不替代 fresh 全厂 census。 |

## 燃料与石墨边界

唯一权威燃料记录为[燃料持续性就绪复核](evidence/2026-10-01/fuel-continuous-readiness-check.md)：预声明至少 1 棒/分钟未通过；重叠采样给出的速率上界为 0.794 棒/分钟，不能当作实际速率。氘供应/分配的物理修复未定位，不降低持续门。

本阶段只读诊断观察了石墨与氘/重氢设备的瞬时状态，不能据短窗宣布全厂停机或唯一长周期因果。复用的 5315 对象场景是缓存快照，不是 fresh 全厂预检；现有图上存在通向下游的 reciprocal 路径，但 5187 与其他石墨负载共用 belt 3365 是否造成竞争仍是假说。详见本阶段[Drive、存档与燃料/石墨边界](evidence/2026-10-01/drive-four-save-and-fuel-graphite-boundary.md)。

## Foundry 只读草案

Foundry 对 1210、目标 1/min 的深度 3 计划返回 `executable=false`：金刚石 4/min → 引力透镜 1/min → 空间翘曲器 1/min。依赖奇异物质 1127 的自动外供，尚未证明；物流、场地、库存预算和供电也未验证。建筑物品 2302×1、2303×2 不是实体 ID 或整链预算。该读取不是 native preflight、施工或持续产出证明，也不授权开始建造。

## 回归与计量边界

在本机实际 `pwsh` 与私有 .NET SDK 8.0.424 下，locked Core restore、Release build、2416 个 Core/Contracts/MCP tests、完整 `Spherewright.sln` Release build 均通过，构建 0 warnings / 0 errors。早先经 PATH dotnet 启动的四个入口（restore、build、test、full build）都因找不到 SDK 以 `-2147450735` 退出；这是四次 runtime caller startup failure，不是编译/测试失败。局部阶段时间与原日志索引见事件记录；不推算端到端提速或 provider/model 用量。

本阶段共 47 次 Game native calls（45 只读、save prepare/commit 各 1），无 native refusal。writer 两项 DTO 摘要错误中，首项曾补读 session/progression 两次，第二项离线核销；root 三项审计解析修正均零 Game calls。首个诊断响应到保存 prepare 响应相隔 52.337 分钟，而 save leaf 约 1.651 秒；本轮未交付生产修复，不宣称效率提升。没有 accepted 动作重放；本机回归 Game calls 为 0。无新 Plugin/MCP surface、安装或部署变化。

## 未完成目标与安全边界

石墨/氘燃料仍未通过持续门；Foundry 的三级供应计划仍是不可执行只读草案，自动 Matter 外供及中途恢复未验证。紫糖 1402→4743→6004→Lab84 仍未端到端完成；2104 已由历史库存解锁，不得据此宣称紫糖路线通过或擅自改选目标。未证明持续燃料、Foundry 实体建设、真实重启或最终双包验收。

只对精确核验的 owned identity 执行有界动作，遵守 fresh prepare、计划校验、唯一 commit、同 action terminal/readback 与 durable Journal。accepted 不因 revision、tick 或 Journal 变化清零；摘要异常或断线不授权重放。遇到 unknown、quarantine、身份/版本漂移或结果无法核销时冻结新写并交 root。凭据、token、真实存档名、绝对私有路径和 raw body 不入库。
