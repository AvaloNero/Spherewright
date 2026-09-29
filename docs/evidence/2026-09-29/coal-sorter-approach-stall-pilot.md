# 煤区分拣器接近试跑：短路段停滞

日期：2026-09-29（Asia/Singapore）。源码与安装批次沿用[29104 原档恢复](dsp-29104-owned-primary-live-recovery.md)；本次没有代码、安装或发行包变化。

## 已发生的有限动作

- 受保护恢复 `bd88bb62-7558-484b-96c8-df6a4f03543a` 已终态成功，同一 owned 主档正常保存于 `77676475 / J91`，新 `GameData` 会话的 revision 为 `1`。这是本外部窗口从 accepted `8→9` 的动作；恢复及保存是该动作的一部分，不另计一次。
- 原计划升级煤分拣器 `5170`，但当前玩家距目标约 `136.6 m`，原生升级 prepare 返回 `TARGET_OUT_OF_RANGE`，没有 plan/commit。该设备的旧过滤 `1006`、双端 `5163→5170→5167` 和满额供电已作只读确认；背包有两只高速分拣器。没有升级它。
- 朝历史目标的第一条约 `20 m` Move 只读预检，末五个地表采样位于水下，`shoreRisk=detected`，未提交。随后比较的两条正交约 `20 m` 候选各有 `21` 个 observed、`0` 个 unknown 地表采样，`shoreRisk=not_detected`。这只排除所采样地表的显见岸线风险，不能证明建筑/植被碰撞或原生控制器必能通过。
- 只对其中一条新鲜计划提交一次普通 Move：目标 `(-100.52059,-78.66576,-154.25521)`，action `a956e26b-e599-4e8d-b6f2-dda301f79dcd`。原动作于 `77807735→77807949` 结束，`terminal=true / succeeded=false / state=action_failed / failureKind=position_stalled`；`stalledGameTicks=181`、`remainingDistance=16.133113861083984`、`doNotRetrySameTarget=true`、`recoveryRequired=false`。结构化建议为 fresh 读玩家和近处几何；能识别单障碍时背离障碍约 `5 m`，否则至多四个正交约 `4 m` 短目标、每方向一次，并逐 action 等终态及复读 Walk/低速/能量。原回执的脱敏证明 SHA-256 为 `F45A3426803A2CFDDF57991A81C7FB384E68DC10876E9991ABBDCBEF2F7E7FC6`。

## 边界与下一步

这笔失败但已接受的 Move 把外部计数 `9→10`，因此下一笔游戏 commit（包括 save）前冻结。没有重放失败目标、执行另一正交候选、升级设备或发出保存。Move 后只读结果为同一 owned/healthy 星球 `104`、`Walk`/速度 `0`、充足核心能量、背包与 `J91/91` 无新增事件或持久化错误；玩家停在约 `(-97.59558,-64.98632,-162.292389)`。最后已保存主档仍是 `77676475`，该约 `3.9 m` 的位置变化尚未落盘。没有 quarantine、outcome unknown 或回档。

停滞确由原生移动 watchdog 识别；具体阻挡物尚未唯一定位，不能写成已证实是着陆基座、某台建筑或水面。下一步先完成十写只读严格审计、文档、推送和绿 CI，由主会话封窗；之后重新设计**不同**的有界短目标，复核附近几何，再交 Luna 单流执行。干燥采样不是可通行保证，不能为赶到 `5170` 反复撞同一目标。此试跑不是煤供给、石墨、黄糖或 `0.4.0` 验收。
