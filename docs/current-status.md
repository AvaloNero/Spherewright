# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 资源补证run `9a1bfc2fb446453c8013086fa3c75354` ord7：80970975 /R3/healthy，同session `41828547-0873-415e-a6aa-b90cbead076a` | 全厂快照另为80936286；不是保存tick |
| 最近正常保存 | **80731225**，ownedSaveState=saved，新protected ticket已核 | native恢复action内部正常save；不回滚到旧80526460 |
| durable Journal | 全厂采集run `6207d5a744894c11b60f654163aee38b` ord56：**J96 durable**、无pending/error；96原事件逐条一致 | 不由revision或accepted推算 |
| 外部写窗口 | accepted **10/10，冻结**，无在途；root独立审计已通过 | 等本事件commit/push/准确SHA CI green/root明确交接；不提前reset |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 恢复边界与下一步

源尾已原生延伸三带：`5202→5201→5200→5204→5203→5205`，正常耗材367→364；消费者`5198→5197→5199→84`保留。完整53页/5205built/0prebuild/10146互返有向边已核，新增仅9对象；除声明的配置/连接变动及共享vein313消失外无其它旧静态差异。原生pending=0；执行摘要的标量Count与施工前快照误用不是真blocker。[施工与十写事件](evidence/2026-10-01/purple-source-dogleg-ten-write-audit.md)。

新窗首先正常保存本次三带，再fresh验证普通分拣器短跨接和后续主干。1932/1934旧带跨接、仓库出口、紫糖到Lab84及持续科研仍待；不重试原路径、不拆成功前缀。progression真实2104为机甲核心4（hash0/300000），2904才是未解锁驱动引擎4；没有完成曲速准备。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[只读恢复检查与错误字段事件](evidence/2026-09-30/owned-recovery-readonly-and-error-fields.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
