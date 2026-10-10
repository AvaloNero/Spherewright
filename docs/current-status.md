# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`（Windows Core CI `37992232537` success；2,762 项测试、228 个文件、64 个 MCP tools、1 个 resource）。本页只记当前状态；来源与持续供给门仍未通过。

## 当前运行与窗口

- 当前 owned 世界为 P104/R23，最后正常主档保存 tick `105510911`，durable Journal 为102。新固定20窗口从 lifetime 432 开始，目前1/20、lifetime433；R652唯一accepted移动以 `action_failed/position_stalled` 结束，尚无新保存覆盖本窗动作（`allCurrentActionsSaved=false`）。窗口冻结；无在途或未核销unknown。
- 整案累计34 accepted、4次移动和7次普通保存；其余已核销的北向路线与材料累计保持不变。玩家Rod库存0、Warp库存215。R665最近观察到被动漂移0.129m、CoreEnergy 1.59998 GJ；该读数不代表后续状态未变。
- R657因速度0.163551718高于0.15而在prepare前停止。R661的drift prepare虽为positive，但Native在创建动作前返回 `STALE_STATE`，0 accepted；R664通过5次只读核验关闭调用方本地未决，未清窗口计数、未保存。

## 石料来源与物料边界

- 既有R641接缝和R643一次有限送达仍保留；`sourceAdmitted=true` 仅表示那次送达，不证明连续来源。R659来源核验中，20个当前有效有限矿点为正例；历史节点36/37返回 `INVALID_ENTITY`、剩余量不可用，不能当作剩余为0。矿机1213当前资源列表为空，也不足以断言煤源枯竭；铁源1496仍有节点51/56。
- R659来源清单保留了既有29项物料、路径与燃料覆盖。Warp仓5331为3000，Rod仓3955为817，玩家Rod为0；五根携带棒及正式样本消耗尚未执行。861的石料16、精炼油12、水8、硫酸78只是有限缓冲。

## 验收边界

- `wholeSupplyPassed=false`，continuous credit为0。Warper与Rod各至少1/min、连续36,000 ticks、双自动补给、完整来源竞争与材料供需、整合保存恢复和最终同 SHA 双候选包验收仍未通过；任何历史有限送达、库存或额定容量都不能替代这些门。
- R508双包检查仍只是离线预检，不是实际 Mod Manager 安装或最终发行验收。

阶段索引：[漂移移动与石料来源边界](evidence/2026-10-10/warehouse-return-recovery-r665.md)、[石料接缝、送达与来源诊断](evidence/2026-10-10/stone-source-seam-power-save-r641.md)、[来源书挡与准备清单](evidence/2026-10-09/full-source-bookends-r441.md)。
