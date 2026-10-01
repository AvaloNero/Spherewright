# 四级驱动引擎保存与燃料/石墨边界

日期：2026-10-01（Asia/Singapore）。本记录合并一个已核正常保存、两段燃料/石墨只读观察和一项 Foundry 只读草案；不表示 0.4 验收、实体建造、持续生产、部署或存档恢复完成。

## 源码、安装与已保存进度

本阶段 pinned source 为 `b3264af9812552c359c7f6331d35dcca283d9b7e`；安装仍是 DLL cohort `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`，DSP 0.10.35.29104 / 228 files / 64 tools / 1 resource。root 比较该安装来源到 pinned source 的 `src`、`tests` 差异为空，没有重新部署。

科技进度先后读回：2104（机甲核心）于 tick 83027891 解锁，2904（四级驱动引擎）于 tick 84027632 解锁；两者均 4/4 满、科研队列为空，并被本阶段正常保存覆盖。唯一 save action `f220bd0d-b67a-4200-9ea5-e547333de106`，原回执 run `e5569b4535294f129769a083cac4b02e`；root proof `c289b84ae8cc41109b03763641833bc4` 核验 11 个原始响应成功。保存 tick 84232813、session R6→R7，保存后 Journal 读回 J97 durable；后续 session 观察 tick 84244696 / R7 / healthy / lastSaved 84232813，不是同 tick 的 Journal 采集。external accepted 从 7 增至 **8/10 OPEN**；本阶段仅此一项新增 accepted，无 unknown、无在途写、没有本阶段施工写入；未做 fresh 全厂 census，不能由此保证世界全局实体未变。没有实际重启/恢复验证。

## 燃料与石墨只读观察

18-call 诊断 `b021765…` 的 root proof 为 `b1bbadfa991742cbbf347bbdcaf327f5`；后续 12-call 诊断 `db2e5b50…` 的 proof 为 `655ca6a21d8f4116b9fd37637d616a6f`。后者原始读取成功、root 审计没有新增 Game calls。观察到 Graphite 路径有 3083/3965/3966 相关阻塞信号，而 3084 报 working；3073 曾读到 H8、D output 0，3403 alloy 2、D input 15，ring 2。另一段 600-tick 窗为 84176702–84177301：H 计数 9/4，D 0/0，rod 0/0，graphite 2/3。短窗只描述采样边界，不用于推断唯一长周期因果或整厂停机；3074 DTO 未提供数量，不能写成空仓。

同期热电设备 3060–3063 的 `isWorking=false`，但实测 Graphite 发电分别为 2948/2948/2936/2945 J/t；3404 环输出缓存为 10 满，5187 Graphite 缓存为 100 满，network 3 ratio 为 1、generatorRatio 为 0.07244。这些是原生采样值，不是持续供电或产量证明。

拓扑分析复用了既有完整 5315 场景 `da57094d…`，它是缓存静态快照而非本轮 fresh preflight/census。root 的 H allocation proof `2e1d5e37be9c437c923d82eafa4fdce6` 核对四个 H 来源经 3074→3073 的既有 reciprocal 路径；先前 route proof `a73c82ed793045d6bf70ed264b2efe81` 分别核对 H 输入和重氢 3073→3074→3403 的输出路径，不能把重氢边误标为氢边。Graphite route proof `b91b156a613c4d529bd1912738628d51` 核对 3083 通向热电/磁环机的路径。5187 在 belt 3365 合流，负载竞争目前仅是假说。静态路径不能替代 fresh buffer、现场供料或连续窗口证据。

此前燃料持续性门要求至少 1 棒/分钟，未通过。不同样本窗口有重叠，covered-produced 计数之和 8 只提供窗口产量上界，对应速率上界 ≤0.794 棒/分钟，不是精确实际速率；不得用短窗或单帧状态降低门槛。完整边界见[燃料持续性事件](fuel-continuous-readiness-check.md)。

## Foundry 三级只读草案

Foundry 原始读取 run `4e66dc92a6fa44e9b07fbcdb2331c685`：ordinal 1 `get_foundry_plan`、ordinal 2 session；proof `3e555d7d0c80435f82a8bcebd3142bb7` 独立核对这两条成功读回。草案 target `1210`，目标 1/min，depth 3/3，计划功率 900 kW，material plan `executable=false`，hash `af1431bb…40d`。

计划链为 recipe 60 / building item 2302：金刚石 1112 @ 4/min ← 高能石墨 1109 @ 4/min；recipe 101 / building item 2303：引力透镜 1209 @ 1/min ← 金刚石 4/min + 奇异物质 1127 @ 1/min；recipe 78 / building item 2303：空间翘曲器 1210 @ 1/min ← 引力透镜 1/min。2302/2303 是建筑物品 ID，不是已建实体 ID。`machineCost` 2302×1、2303×2 只是机器数量成本，不含物流、供电、场地或库存预算；1127 自动外供尚未证明。该结果不是 native preflight、可施工计划、完整预算或持续产出验证。closing session 观察 tick 84244696 / R7 / healthy / lastSaved 84232813；本草案只读，没有相应保存需求。

## 回归、调用错误与计时边界

本机实际 `pwsh` 7.6.5 使用私有 .NET SDK 8.0.424 完成：`dotnet restore Spherewright.Core.slnf --locked-mode`（4.781 s），Core Release build（16.394 s），no-build Release tests（15.180 s），以及完整 `Spherewright.sln` Release build（7.120 s）。2416 tests 全通过（59 Contracts / 201 MCP / 2156 Core），构建 0 warnings / 0 errors；本机回归 Game calls 为 0。对应原始日志留在私有 `.local/test-output`，不作为发行或部署证明。

最初经 PATH dotnet 调用上述 restore/build/test/full-build 四个入口均退出 `-2147450735`；日志显示 SDK 未找到。这是四次 runtime caller startup failures，不是四次编译/测试失败。后续固定沿用已验证的本地 SDK 入口。阶段共 47 次 Game native calls（45 只读 + 正常 save 的 prepare/commit），accepted 新增 1。writer 两项 DTO 摘要错误为读取 `technologyId`（实际 `techId`）、session `planetId`（实际 `localPlanetId`）；首项错误后补读 session/progression 两次，已计入 47 次，第二项只离线核原回执。root 三项本地审计错误为 `-eq104` 参数空格错误、把 intent 当 response、误读时钟字段 `recordedAt`（实际 `recordedAtUtc`），修正后仍为零 Game calls。无 native refusal、无 accepted 动作重放。

局部计时分别为诊断 18-call 回执约 5.2 s、12-call 约 1.985 s；save leaf 约 1.651 s / shell 4.270 s；Foundry 两条响应跨度 278 ms / shell 2.334 s。它们口径不同，不是阶段总墙钟。

root ledger proof `5c950a315bc44a2f9af07534edaf2cab` 核验全部 47 个原生响应成功及原记录时钟：首个诊断响应 19:02:20，保存 prepare 响应 19:54:40，相隔 **52.337 分钟**；最后 Foundry/session 响应 19:58:25，原生响应跨度 **56.084 分钟**。这不是委派时间，也不含其后的文档/Git/CI，不据此声称整体提速。保存物理执行很短，但本轮未交付生产修复，结果前的诊断、方案与调用整理仍占用了过长墙钟。完整 dispatch/model/provider usage 未知，不能编造 token 或节省比例。本机回归日志另由 root proof `a892fbb77121499c928aa20159e473bd` 核对四份 SHA-256 与测试/构建摘要，不冒充实机验收。

## 未完成项

唯一燃料物理修复尚未定位，持续 ≥1 棒/分钟门仍未过。Foundry 草案依赖 1127 Matter 外供及尚未验证的场地、库存、物流、供电与中途恢复。紫糖 1402→4743→6004→Lab84 仍未端到端完成；2104 解锁来自历史库存，不是新紫糖路线成果。真实 restart/resume、0.4 发行验收、最终双包、部署均未证明。
