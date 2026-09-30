# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 保存run `dff2724a6d4f42fa8e960e521858c76b` ord8：81554973 /R12/healthy | 同一owned session；不是施工终态tick |
| 最近正常保存 | **81554959**，ownedSaveState=saved，消费者下坡段及分拣器已保存；新的受保护恢复凭据可用 | 尚未做真实重启恢复检查 |
| durable Journal | 保存run `dff2724a6d4f42fa8e960e521858c76b` ord9：**J96 durable**、无pending/error | 不由revision或accepted推算 |
| 外部写窗口 | accepted **6/10**，所有已接受动作有终态，无在途/未核销 | R/J不重置；未冻结，第10写门仍适用 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 消费路径与下一步

已核实下坡八段 `5217→5216→5215→5214→5213→5212→5211→5210`（`2001`、L1→L0），并由 sorter `5218`（item `2011`、filter `6004`）接入既有 `5198→5197→5199→Lab84`。sorter 两端实际槽位与双向连接已核；玩家库存仅耗 `2001×8` 和 `2011×1`，其他15种物品数量/增量不变。调用方的虚拟槽位误报和零库存行省略已通过同一成功 action 的原始读回核正。[消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md)。

上述路径已由正常保存 **81554959** 覆盖，观察 tick **81554973/R12**、J96 durable；新的受保护恢复凭据可用。外部 accepted **6/10**、无在途或未核销。尚未做真实重启恢复检查；上游供料仍未验证，因此没有紫糖到达 Lab84、科研推进或持续吞吐的证明。之前的源尾和高层四带历史证据保留；消费者连接新增不代表整条 supply chain 完成。真实2104仍为机甲核心4（hash0/300000），2904才是未解锁驱动引擎4；没有完成曲速准备。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
