# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`（Windows Core CI `37992232537` success；2,762 项测试、228 个文件、64 个 MCP tools、1 个 resource）。本页记录当前运行状态；来源与持续供给门仍未通过。

## 当前运行与窗口

- 当前 owned 世界为 P104，最后正常主档保存 tick `105708553`，durable Journal 为 102。R672 独立核销后，固定20窗口累计 `2/20`、lifetime `434`；整案累计35次 accepted、4次移动、8次普通保存。窗口提前封存，18个未用槽位退役，不转移到新窗口。
- R652 的失败移动保留为已accepted历史动作；本次正常保存覆盖了该动作。当前无在途、未核销unknown或quarantine。R672确认玩家仍处于Drift，不能据此认定已落地或抵达仓库。
- R672完整工厂快照为 `6475 built / 0 prebuild`、65页；静态配置与连接无变化，N1、N3、N4网络均满供，库存保持不变。该快照和封窗审计不提供持续供给或仓库到达证明。

## 移动与来源边界

- R666的40秒观察取得120个Drift读数，但严格0.03米边界未满足，未尝试Native移动。R665恢复候选族已有1次Native拒绝，候选上限为2；不将其写成恢复或到达成功。
- R659列出的20个当前有效有限矿点有正剩余；历史节点36/37返回 `INVALID_ENTITY`，剩余量不可用。矿机1213当前资源列表为空不足以断言煤源枯竭；铁源1496仍有节点51/56。R668复用了完整29项物料和拓扑上下文，但没有生产tick信用。

## 验收边界

- `wholeSupplyPassed=false`，continuous credit为0。Warper与Rod各至少1/min、连续36,000 ticks、双自动补给、完整来源竞争与材料供需、整合保存恢复和最终同 SHA 双候选包验收仍未通过；历史有限送达、库存或额定容量不能替代这些门。
- R508双包检查仍只是离线预检，不是实际 Mod Manager 安装或最终发行验收。

阶段索引：[仓库返航、固定窗口封存与石料来源边界](evidence/2026-10-10/warehouse-return-recovery-r665.md)、[石料接缝、送达与来源诊断](evidence/2026-10-10/stone-source-seam-power-save-r641.md)、[来源书挡与准备清单](evidence/2026-10-09/full-source-bookends-r441.md)。
