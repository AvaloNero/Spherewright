# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 材料保存run `f7ba99c53f97448181de6ec8699c79eb` ord27：81634743 /R16/healthy/resumeAvailable | 同一owned session；不是施工终态tick |
| 最近正常保存 | **81634733**，ownedSaveState=saved，材料取用与六个分拣器手搓已保存 | 尚未做真实重启恢复检查 |
| durable Journal | 保存run `f7ba99c53f97448181de6ec8699c79eb` ord28：**J96 durable**、无pending/error | 不由revision或accepted推算 |
| 外部写窗口 | accepted **9/10**，所有已接受动作有终态，无在途/未核销 | 尚未冻结；第10写门仍适用 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 消费路径与下一步

已核实下坡八段 `5217→5216→5215→5214→5213→5212→5211→5210`（`2001`、L1→L0），由 sorter `5218`（item `2011`、filter `6004`）接入既有 `5198→5197→5199→Lab84`。本次从仓 `1511` 原生取6块铁并手搓出6个分拣器：转移 action 同步读回仓内 `3000→2994`、玩家 `0→6` 守恒；手搓读回铁 `6→0`、电路 `999→993`、分拣器 `0→6`，带 `2001` 为352，队列空，其他15种数量/增量不变。后续仓读为2995，较该 action 的post-transfer值多1、来源未归因；不能据此宣称仓的当前净变化恰为−6。[紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md)。

上述新材料由正常保存 **81634733** 覆盖，观察 tick **81634743/R16**、J96 durable；外部 accepted **9/10**，无在途或未核销、未冻结。真实重启恢复尚未验证。已建下游仍到 Lab84，但上游供料未连通；六个分拣器目前只是背包备料，不证明紫糖到货、科研推进或持续吞吐。之前的源尾和高层四带历史证据保留。真实2104仍为机甲核心4（hash0/300000），2904才是未解锁驱动引擎4；没有完成曲速准备。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
