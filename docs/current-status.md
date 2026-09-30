# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。此文件**覆盖更新**，不是日记或权威机器状态；实时身份、revision、Journal、accepted 和动作状态仍以 fresh 原回执及外部台账为准。历史原文已归档至 [迁移前的累积状态](evidence/2026-09-30/current-status-history-through-2026-09-30.md)，逐档时间线见 [存档日记](gameplay-timeline.md)。

## 版本与保存边界

| 项 | 最后已核证值 | 边界 |
|---|---|---|
| 开发源码 | 最近已验证的游戏阶段基线 `e3fdbce`；流程优化的准确源码提交以 `git HEAD` 为准 | 文档/脚本后续提交**不是**已安装游戏批次，不能据此推断 Plugin/MCP 已变 |
| 游戏/安装 | 受保护回执显示运行中 DSP `0.10.35.29104`；当前已安装 Plugin/MCP 的**精确 SHA 本次未重验** | 不把源码 HEAD、旧包或文档修改视作冷部署；工具面/哈希以最后一次安装记录及下一次实际检查为准 |
| 正式发行 | `v0.4.0` 未 tag、未 Release、未发布 Thunderstore | [Roadmap](../ROADMAP.md) 的全部验收门仍适用；旧 0.4 本地候选包不是正式包 |
| owned world | `owned-world-001`，母星104，最后正常保存 **tick80273796** | 保存 action 终态成功；fresh ord19 为 owned/saved/healthy，protected resume advertised；本次保存后**未验证重启恢复** |
| 最近观察 | 只读run `aa454891493d4e11b2ce6a24c88bf7f3/ord75`：session **tick80282748 / revision28** | `lastSaved=80273796`；观察未改保存，也不等于连续生产或重启恢复验收 |
| Journal | 同run ord77：tick80282749，**durableThroughSequence96**、无 pending、无错误 | 只读观察期间Journal保持durable；与revision/accepted不同 |
| 外部写审计 | **accepted 8/10**，尚余2个写槽 | 三窗观察新增accepted=0；#7分拣器升级、#8正常保存仍为最近两次写；达到10仍须完整独立审计和绿CI门 |

## 已完成与未证明

- 前一窗口新增矿机5171、煤带5172–5186、炉5187、煤过滤输入5188、石墨出炉带5189–5194，严格十写审计通过、无额外静态厂区差异；见 [前一封窗证据](evidence/2026-09-30/graphite-outlet-extension-ten-audit.md)。
- 本窗两次正常保存包住两条 fresh 原生分拣器施工：5193→旧石墨带3365 的5195，以及炉5187→带5189 的5196。四个 accepted action 均 terminal/succeeded；主会话独立核原终态、保存、owned/健康、J95、零预建筑。两端槽位互返与材料变化由 Luna 原受保护读回证明；见 [本阶段原索引和界限](evidence/2026-09-30/graphite-inlet-two-sorters-save.md)。
- 既有生产诊断：旧带3365有石墨、3404已有输入且满输出；3403重氢来源已追到3073粒子对撞机recipe40，氢输入不足一次10件批次。黄糖774满输出10/10，778已存6000，不能把零产量直接归为原料不足。三窗燃料0/2/0仍不是持续供应；见[三窗边界](evidence/2026-09-30/graphite-fuel-three-window-consumer-blocker.md)。
- 共享科研入口约4.97秒完成原生2104排队、核验及正常保存，queue/currentTech均2104、尚未解锁；J96记录首次选择。主会话独立核两个终态及预算/队列/保存/J；见[单份科研阶段证据](evidence/2026-09-30/core2104-shared-research-save.md)。
- 分拣器3068由2011局部升级为2012后正常保存于80273796；buffer前后均空，故不证明携货升级或链路恢复。fresh/R28与J96已核，升级后持续产量及重启恢复未测；见[本阶段事件与边界](evidence/2026-09-30/hydrogen-exit-upgrade-save.md)。
- 升级后只读诊断完成三个互不连续的600-tick窗：1121各产5（30/min），1802燃料为0/0/2，3064氢589/587/587；3073供氢和3403燃料仍未证明持续。功率仅证明采样点，3413是2001传送带而非仓库；见[三窗诊断与原回执索引](evidence/2026-09-30/hydrogen-upgrade-three-window-diagnostic.md)。
- 历史已核销：2026-09-10六对象蓝图复制/取消/原buildId恢复续建和原31→62、±10%吞吐连续37128ticks，见当档日记对应日期；不重做成功模块，不据此核销三级链、完整配平或最终当前版本回归。

## 当前 blocker、动作和下一阶段

- 当前硬边界：三窗中3064氢为589/587/587，1121仍各产5（30/min），1802燃料为0/0/2；3073前两窗有氢9且idle，第三窗working时氢0，3403的D为0/5/10，持续燃料供应未通过。2104仍在队列、`hashUploaded=0`；lab84紫糖科研点0，储仓3051有紫糖2288、输入5113且无输出，当前科研阻塞是缺少到lab84的实际输送，不是没有生产。三窗是间隔的诊断样本，不拼成连续生产；见[原索引与边界](evidence/2026-09-30/hydrogen-upgrade-three-window-diagnostic.md)。
- 本窗八个写动作均terminal/succeeded、无在途或未核销；只读三窗命令23857 exit0，新增accepted=0，累计8/10、余2写槽。三窗已完成，不重复采样或盲目升级。下一硬接口由主会话先做lab84最小消费者端口原生预检，再按结果决定上游；不重建紫糖/黄糖流水线。旧5192→3362/3365 `TooSkew` 目标不原样重提，已成功5193–5196及本次科研/升级/保存禁止重放。
- 新写仍须fresh核owned/session/版本/J及外部accepted8/10；还有2个审计槽，达到10即冻结完整审计。当前保存后的重启恢复、分拣器升级后的持续产量与三级链/最终双包待核销，不重复历史通过模块。

## 证据入口

[当前规则](../AGENTS.md) · [Roadmap](../ROADMAP.md) · [Agent playbook](agent-playbook.md) · [存档日记](gameplay-timeline.md) · [十写审计](evidence/2026-09-30/graphite-outlet-extension-ten-audit.md) · [本窗保存与连接](evidence/2026-09-30/graphite-inlet-two-sorters-save.md) · [分拣器升级与保存](evidence/2026-09-30/hydrogen-exit-upgrade-save.md) · [升级后三窗诊断](evidence/2026-09-30/hydrogen-upgrade-three-window-diagnostic.md)。受保护原回执只在本机证据库中，提交文档只保留脱敏索引；独立验收不能只读本摘要。

已完成的[最小流程优化](evidence/2026-09-30/minimal-workflow-efficiency.md)不扩大游戏写入或验收权限。
