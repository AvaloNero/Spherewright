# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 恢复核销run `669740f987b54a2f9f073a370b4578b0` ord1：80766832 /R1/healthy，newsession `41828547-0873-415e-a6aa-b90cbead076a` | 不是保存tick；下一写前仍fresh read |
| 最近正常保存 | **80731225**，ownedSaveState=saved，新protected ticket已核 | native恢复action内部正常save；不回滚到旧80526460 |
| durable Journal | 核销run ord11：原Journal ID、**J96 durable**、无pending/error；96原事件逐条一致 | 不由revision或accepted推算 |
| 外部写窗口 | accepted **9/10**，无在途 | 恢复只新增1；第10accepted后仍冻结/独立审计/commit/push/green交接 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 恢复边界与下一步

恢复后已独立核8对象×14静态字段无差异、完整16行玩家材料相同（belt367/sorter1）、96原Journal事件连续。施工前缀`5198→5197→5199→84`与`5202→5201→5200`保留；这不是全厂十写审计。唯一物流blocker是主干在1932处原生碰撞；不重试原路径、不删端点、不重做成功前缀。3051紫糖1824、Lab84紫色研究点0，仓库出料/实际送达/持续科研仍未证明。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[只读恢复检查与错误字段事件](evidence/2026-09-30/owned-recovery-readonly-and-error-fields.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
