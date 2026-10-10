# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`（Windows Core CI `37992232537` success；2,762 项测试、228 个文件、64 个 MCP tools、1 个 resource）。来源接入已取得有限正证，持续供给与最终发行门仍未通过。

## 当前运行与窗口

- 当前 owned 世界为 P104/R64，正常主档 Save `106084429`、durable Journal 102。新固定 20 写窗口从 lifetime 454 开始，目前 external `5/20`、整案 lifetime `459`；本阶段 5 个唯一动作均已由该 Save 覆盖。整案累计 60 次 accepted、15 次移动、5 次手工材料动作、15 次普通保存、173 条新带、3 个分拣器和2台矿机；计数不清零、不转移。
- 完整工厂为 6492 built/0 prebuild；本次核验没有新增、移除、静态变化或非互惠连接，P104 本地 N1/N3/N4 网络满供且成员/容量保持。Native 版本 29104；执行 cohort 未变化。
- 当前背包 Warp 300、Rod 5。此次 Warp 从既有仓库 5331 有限取料 85，使背包从 215 增至 300；这是现存库存转移，不是新生产或自动补给。Rod 5 仍为有限携带库存。Stone 6 的来源仍未归因，获取/供给信用为 0；不假定其来自手动操作。

## 来源与供需观察

- R757 对新接入铁源的有限核验确认 `sourceAdmitted=true`，并在三个相互分隔的 600-tick 样本中观察到实际流动；这些样本不是连续窗口。R763 的完整关窗审计证明本次保存与全厂状态，没有扩展来源持续性结论。详见[完整来源核验](evidence/2026-10-10/iron-source-flow-and-full-readiness-r757.md)与[R763 窗口关闭和出发材料截面](evidence/2026-10-10/rod-carry-and-window-closure-r763.md)。
- R766 的三条正常 28 m 目的路线与有限 Warp 取料均已保存；路线预览只提供向下采样，不能证明路线净空或 dryland。该阶段未施工，也不增加供给或产率信用，详见[Warp 携带量、接近路线与保存核销](evidence/2026-10-10/warp-carry-approach-and-save-r766.md)。
- 既有 R682、R699、R704、R743 与 R753 的有限资格、建造及保存记录仍作为历史证据保留；current-status 不把它们写作新的现场读数。R729 已有 ≥100 MJ 到达储备的证据，但不证明 dryland。Stone 6 仍无来源归因。

## 验收边界

- `wholeSupplyPassed=false`，continuous credit 为 0。Warper 与 Rod 各至少 1/min、连续 36,000 ticks、双自动补给、完整来源竞争与材料供需、整合保存恢复和最终同 SHA 双候选包验收仍未通过；有限送达、库存、额定容量及断续窗口不能替代这些门。
- R690 地形预览中的多数位置位于水下；Walk 速度为 0 不能代替 dryland/上岸证据。旧 orthogonal 4 m 候选族的两次拒绝仍退役；R695 旧采煤候选有一次 Harvest prepare `STALE_STATE`，不能称为第二次拒绝。后续 R717 原 Harvest 在 caller 等待超时后由 R720 核为成功，R722 又正常加燃料并保存。R729 到达储备已满足，但不代表上岸或完成供料。
- R736→R737 的自由 Native `grid/2001` 侧段与普通分拣器正例、R753 的铁源接线以及 R757 的有限流动观察均保留原范围；没有把它们升级为整案持续供给通过。固定 36,000-tick 验收门不变。
- R508 双包检查仍只是离线预检，不是实际 Mod Manager 安装或最终发行验收。

阶段索引：[R766 Warp 携带量、接近路线与保存核销](evidence/2026-10-10/warp-carry-approach-and-save-r766.md)、[R763 窗口关闭和出发材料截面](evidence/2026-10-10/rod-carry-and-window-closure-r763.md)、[铁源流动与完整准备核验](evidence/2026-10-10/iron-source-flow-and-full-readiness-r757.md)、[铁矿来源接入与保存](evidence/2026-10-10/iron-source-connection-and-save-r753.md)、[Iron40矿机与已资格侧接缝](evidence/2026-10-10/iron40-miner-and-qualified-seam-r743.md)、[近铁源有限到达与保存核销](evidence/2026-10-10/near-iron-source-arrival-r729.md)、[完整来源只读核验与铁源差异](evidence/2026-10-10/full-source-readiness-r682.md)、[漂移状态与来源边界](evidence/2026-10-10/warehouse-return-recovery-r665.md)、[来源书挡与准备清单](evidence/2026-10-09/full-source-bookends-r441.md)。
