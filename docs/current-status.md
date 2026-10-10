# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`（Windows Core CI `37992232537` success；2,762 项测试、228 个文件、64 个 MCP tools、1 个 resource）。本页记录当前运行状态；来源与持续供给门仍未通过。

## 当前运行与窗口

- 当前 owned 世界为 P104，最后正常主档保存 tick `105708553`，durable Journal 为 102。R672封存的前一固定20窗口从lifetime432开始，实际2/20、结束于lifetime434，18个未用槽位退役且不转移。R652失败移动保留为已accepted历史动作，并由该保存覆盖。
- 新固定20窗口从lifetime434开启，目前external `0/20`、整案lifetime `434`；整案实际累计35次accepted、4次移动、8次普通保存。R674只把规划的整案普通保存上限从8调至9，未增加accepted或移动上限，实际保存仍为8。当前无在途、未核销unknown或quarantine。
- R673的Drift移动prepare返回Native `STALE_STATE`，0 commit、intent或accepted。R676/R677随后5次只读观察记录速度从 `0.162932277` 到 `0.178838089 m/s`、位移 `0.01617379 m`；Core canonical hash复算仅观察位置变化，其余PlayerAction绑定字段一致，能量变化不是该hash变化原因。内部Native prepare/commit快照不可见，R673拒绝的具体原因仍未证明。当前仍处于Drift；尚未确认人工正常上岸并停稳，也无落地或抵达仓库证据。R672最近完整工厂快照为 `6475 built / 0 prebuild`、65页；静态配置与连接无变化，P104本地N1/N3/N4三个电网均满供，库存保持不变。

## 移动与来源边界

- R666的40秒观察取得120个Drift读数，但严格 `0.03 m/s` 速度边界未满足，未尝试Native移动。R665恢复候选族累计两次Native拒绝，达到上限2并已停止。
- R659列出的20个当前有效有限矿点有正剩余；历史节点36/37返回 `INVALID_ENTITY`，剩余量不可用，不能据此断言枯竭。旧铁矿机1213当前资源列表为空；铁源1496仍有节点51/56。R668复用了完整29项物料和拓扑上下文，但没有生产tick信用。

## 验收边界

- `wholeSupplyPassed=false`，continuous credit为0。Warper与Rod各至少1/min、连续36,000 ticks、双自动补给、完整来源竞争与材料供需、整合保存恢复和最终同 SHA 双候选包验收仍未通过；历史有限送达、库存或额定容量不能替代这些门。
- R508双包检查仍只是离线预检，不是实际 Mod Manager 安装或最终发行验收。

阶段索引：[仓库返航、固定窗口封存与石料来源边界](evidence/2026-10-10/warehouse-return-recovery-r665.md)、[石料接缝、送达与来源诊断](evidence/2026-10-10/stone-source-seam-power-save-r641.md)、[来源书挡与准备清单](evidence/2026-10-09/full-source-bookends-r441.md)。
