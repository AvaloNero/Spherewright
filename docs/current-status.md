# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 保存run `8b55f9337ce34fda89d5b2a1e784518b` ord8：81208018 /R7/healthy，同session `41828547-0873-415e-a6aa-b90cbead076a` | 全厂快照另为80936286；不是保存tick |
| 最近正常保存 | **81208004**，ownedSaveState=saved，高层四带已保存 | 未证明从该保存重启恢复 |
| durable Journal | 保存run `8b55f9337ce34fda89d5b2a1e784518b` ord9：**J96 durable**、无pending/error，96条与前次相同 | 不由revision或accepted推算 |
| 外部写窗口 | 新窗口accepted **3/10**，无在途/未核销 | R/J不重置；第10写门仍适用 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 恢复边界与下一步

源尾已原生延伸三带：`5202→5201→5200→5204→5203→5205`，正常耗材367→364；消费者`5198→5197→5199→84`保留。完整53页/5205built/0prebuild/10146互返有向边已核，新增仅9对象；除声明的配置/连接变动及共享vein313消失外无其它旧静态差异。原生pending=0；执行摘要的标量Count与施工前快照误用不是真blocker。[施工与十写事件](evidence/2026-10-01/purple-source-dogleg-ten-write-audit.md)。

此前低层源尾`5202→5201→5200→5204→5203→5205`保留；另有独立高层四带`5206→5208→5207→5209`，两端自由、内部互返，已正常保存至81208004。sorter预检`5205→5206`/filter6004在native放置检查前被几何拒绝：`nativeChecks=0`，不是已执行`CheckBuildConditions`的拒绝，附件仍未通过。合法高层belt不证明sorter升层；不盲试、不挪近或放宽角度，不拆成功前缀。下一步fresh证明新坡道与sorter接口，不能把计划写成通过；仓库出料、紫糖实际到Lab84及持续科研仍待。[高层前缀、接口与保存事件](evidence/2026-10-01/purple-elevated-prefix-interface-and-save.md)。progression真实2104为机甲核心4（hash0/300000），2904才是未解锁驱动引擎4；没有完成曲速准备。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[只读恢复检查与错误字段事件](evidence/2026-09-30/owned-recovery-readonly-and-error-fields.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
