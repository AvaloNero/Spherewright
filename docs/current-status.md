# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。此文件**覆盖更新**，不是日记或权威机器状态；实时身份、revision、Journal、accepted 和动作状态仍以 fresh 原回执及外部台账为准。历史原文已归档至 [迁移前的累积状态](evidence/2026-09-30/current-status-history-through-2026-09-30.md)，逐档时间线见 [存档日记](gameplay-timeline.md)。

## 版本与保存边界

| 项 | 最后已核证值 | 边界 |
|---|---|---|
| 开发源码 | 最近已验证的游戏阶段基线 `e3fdbce`；流程优化的准确源码提交以 `git HEAD` 为准 | 文档/脚本后续提交**不是**已安装游戏批次，不能据此推断 Plugin/MCP 已变 |
| 游戏/安装 | 受保护回执显示运行中 DSP `0.10.35.29104`；当前已安装 Plugin/MCP 的**精确 SHA 本次未重验** | 不把源码 HEAD、旧包或文档修改视作冷部署；工具面/哈希以最后一次安装记录及下一次实际检查为准 |
| 正式发行 | `v0.4.0` 未 tag、未 Release、未发布 Thunderstore | [Roadmap](../ROADMAP.md) 的全部验收门仍适用；旧 0.4 本地候选包不是正式包 |
| owned world | `owned-world-001`，母星104，最后正常保存 **tick80012591** | 保存 action 终态成功，`ownedSaveState=saved`、write health `healthy`，protected resume advertised；此次保存后**未验证重启恢复** |
| 最近观察 | 只读 run `58687b5685714196b502be2ac9afb170/0004–0006`：session **tick80053207 / revision22**，player80053776，Journal80053780；最后零预建筑查询仍80012597 | 观察晚于保存，不能将观察 tick 当作 durable save tick；本次没有重采工厂或验证重启，继续写前必须 fresh 读取 |
| Journal | **durableThroughSequence95**、`persistencePending=false` | 本阶段无新的首次事件；Journal 与游戏 revision/accepted 是不同计数 |
| 外部写审计 | 新窗口 **accepted 4/10**，未触发冻结 | 前一10写窗口审计、push、绿CI后才开的新窗口；此值只在原 action 台账继续核算，绝不重置 tick/revision/J |

## 已完成与未证明

- 前一窗口新增矿机5171、煤带5172–5186、炉5187、煤过滤输入5188、石墨出炉带5189–5194，严格十写审计通过、无额外静态厂区差异；见 [前一封窗证据](evidence/2026-09-30/graphite-outlet-extension-ten-audit.md)。
- 本窗两次正常保存包住两条 fresh 原生分拣器施工：5193→旧石墨带3365 的5195，以及炉5187→带5189 的5196。四个 accepted action 均 terminal/succeeded；主会话独立核原终态、保存、owned/健康、J95、零预建筑。两端槽位互返与材料变化由 Luna 原受保护读回证明；见 [本阶段原索引和界限](evidence/2026-09-30/graphite-inlet-two-sorters-save.md)。
- 炉配方17有煤输入2、石墨输出84；带5189/5193各有单点石墨观察，旧带3365当次为0。**没有**已到旧磁环3404、持续石墨/磁环/红黄糖恢复、完整保存后重启或 0.4 准备门通过的证明。不能拿单点货物、缓存下降或机器数量当持续产量。

## 当前 blocker、动作和下一阶段

- 当前硬边界：新旧带接口已建，但“石墨真正进入旧消费者并持续加工”尚未完成连续实验；只读已确认3404 recipe103→1205超级磁场环、3403 recipe41→1802氘核燃料棒。连续实验未开始，不得用1209/1121替代产出身份或为了孤立上游继续扩建。
- 截至本次受保护读回，四个批准动作全部 terminal，没有已知在途施工/未核销 action；Luna 已停止游戏推进。若之后发现原回执或现场冲突，立即冻结并核同一 action。旧失败的5192→3362/3365原生 `TooSkew` 目标不得原样重提；已成功5193–5196不得整段重放。任何新写仍须 fresh prepare、唯一 commit、同 action 终态和材料/连接复读。
- 本轮任务仅做本地流程优化和离线测试，不自动加载、部署、追加施工或发行。下一次游戏任务须先 fresh 核 owned/session/版本/J 与外部 accepted 4/10，再按批准范围执行；上次保存后的重启持久性仍待单独实机核销。

## 证据入口

[当前规则](../AGENTS.md) · [Roadmap](../ROADMAP.md) · [Agent playbook](agent-playbook.md) · [存档日记](gameplay-timeline.md) · [十写审计](evidence/2026-09-30/graphite-outlet-extension-ten-audit.md) · [本窗保存与连接](evidence/2026-09-30/graphite-inlet-two-sorters-save.md)。受保护原回执只在本机证据库中，提交文档只保留脱敏索引；独立验收不能只读本摘要。

本轮只做[最小离线流程优化](evidence/2026-09-30/minimal-workflow-efficiency.md)，新增accepted=0；Luna已停止、无活动句柄/在途/未核销动作。新模板不是加载、施工、独立验收或发行授权。
