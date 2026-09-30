# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。此文件**覆盖更新**，不是日记或权威机器状态；实时身份、revision、Journal、accepted 和动作状态仍以 fresh 原回执及外部台账为准。历史原文已归档至 [迁移前的累积状态](evidence/2026-09-30/current-status-history-through-2026-09-30.md)，逐档时间线见 [存档日记](gameplay-timeline.md)。

## 版本与保存边界

| 项 | 最后已核证值 | 边界 |
|---|---|---|
| 开发源码 | 最近已验证的游戏阶段基线 `e3fdbce`；流程优化的准确源码提交以 `git HEAD` 为准 | 文档/脚本后续提交**不是**已安装游戏批次，不能据此推断 Plugin/MCP 已变 |
| 游戏/安装 | 受保护回执显示运行中 DSP `0.10.35.29104`；当前已安装 Plugin/MCP 的**精确 SHA 本次未重验** | 不把源码 HEAD、旧包或文档修改视作冷部署；工具面/哈希以最后一次安装记录及下一次实际检查为准 |
| 正式发行 | `v0.4.0` 未 tag、未 Release、未发布 Thunderstore | [Roadmap](../ROADMAP.md) 的全部验收门仍适用；旧 0.4 本地候选包不是正式包 |
| owned world | `owned-world-001`，母星104，最后正常保存 **tick80012591** | 保存 action 终态成功，`ownedSaveState=saved`、write health `healthy`，protected resume advertised；此次保存后**未验证重启恢复** |
| 最近观察 | 只读 run `ebf65923d61d4025891f3161c1661267/0075`：session **tick80180543 / revision22**；最后零预建筑查询仍80012597 | 三个独立600-tick窗完成；观察不是保存，未验证重启；继续写前仍须fresh读取 |
| Journal | 同run0077：tick80180545，**durableThroughSequence95**、`persistencePending=false`、error为空 | 实验结束的durable边界；与revision/accepted不同 |
| 外部写审计 | 新窗口 **accepted 4/10**，未触发冻结 | 前一10写窗口审计、push、绿CI后才开的新窗口；此值只在原 action 台账继续核算，绝不重置 tick/revision/J |

## 已完成与未证明

- 前一窗口新增矿机5171、煤带5172–5186、炉5187、煤过滤输入5188、石墨出炉带5189–5194，严格十写审计通过、无额外静态厂区差异；见 [前一封窗证据](evidence/2026-09-30/graphite-outlet-extension-ten-audit.md)。
- 本窗两次正常保存包住两条 fresh 原生分拣器施工：5193→旧石墨带3365 的5195，以及炉5187→带5189 的5196。四个 accepted action 均 terminal/succeeded；主会话独立核原终态、保存、owned/健康、J95、零预建筑。两端槽位互返与材料变化由 Luna 原受保护读回证明；见 [本阶段原索引和界限](evidence/2026-09-30/graphite-inlet-two-sorters-save.md)。
- 最新三窗：旧带3365已观察到石墨，3404有石墨输入2/磁环输出10；3403重氢5→10→15，三窗燃料产0/2/0，黄糖均0。新供给并非仍“无石墨”，下一核重氢入口及消费者。详见[单份原证据与边界](evidence/2026-09-30/graphite-fuel-three-window-consumer-blocker.md)；不算持续链、保存恢复或准备门通过。
- 历史已核销：2026-09-10六对象蓝图复制/取消/原buildId恢复续建和原31→62、±10%吞吐连续37128ticks，见当档日记对应日期；不重做成功模块，不据此核销三级链、完整配平或最终当前版本回归。

## 当前 blocker、动作和下一阶段

- 当前硬边界：磁环有成品而下游燃料重氢偏低；先沿3403入口3944/3405/3416核拓扑/供给和原生finding，同时核红黄糖当前消费者，不能继续扩石墨。科技队列空；native2104仍缺含500紫糖的研究，2904依赖它，出发科技未齐。
- 本窗四个动作均terminal/succeeded；只读新增accepted=0。Luna已停止，无活动句柄/在途/未核销；冲突仍冻结核同action。旧5192→3362/3365 `TooSkew` 目标不原样重提；已成功5193–5196禁止重放。新写按当前规则fresh预检和读回。
- 回到0.4主线；上述65请求三窗只读实验已正常结束，无活动句柄或新写。后续Luna只执行root批准有限阶段，fresh核owned/session/版本/J及外部accepted4/10；保存后的重启持久性待实机核销，最终双包尚待全部验收门。

## 证据入口

[当前规则](../AGENTS.md) · [Roadmap](../ROADMAP.md) · [Agent playbook](agent-playbook.md) · [存档日记](gameplay-timeline.md) · [十写审计](evidence/2026-09-30/graphite-outlet-extension-ten-audit.md) · [本窗保存与连接](evidence/2026-09-30/graphite-inlet-two-sorters-save.md)。受保护原回执只在本机证据库中，提交文档只保留脱敏索引；独立验收不能只读本摘要。

已完成的[最小流程优化](evidence/2026-09-30/minimal-workflow-efficiency.md)不扩大游戏写入或验收权限。
