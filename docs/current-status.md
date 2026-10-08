# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R440当前截面与R438观察边界见[钛需求、Warper取料与保存核销](evidence/2026-10-09/ti-warp-demand-save-r440.md)；较早钛需求修正见[R423证据](evidence/2026-10-09/minimum-ti-demand-r423.md)。R323/R324来源条件失败仍保留，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前为健康的同一 owned world；R440正常保存`102781516`/R109/J102，external `15/20`、lifetime `390`，无在途或unknown，普通写入冻结。
- R440独立审计核销此前80条Native写入前缀。R437已成功的普通移动不重做；Warper仓库正常转出85件至玩家，原有仓外71件不抵减本次85件库存门。P104站点1657的TiOre上限为600、916的TiIngot上限为300；旧配置与物理拓扑保留。当前玩家库存相较阶段前仅Warp增加85，其余项目未变。详见[本次事件原件索引](evidence/2026-10-09/ti-warp-demand-save-r440.md)。
- R438同进程只读观察仍在进行，预声明的600-tick窗口、3600-tick采样边界、三厂电力读取及开/收完整物料书挡均未放宽；尚无终态，不把部分观察记作连续生产通过。

## Gate 2 边界

- R440通过的是前缀审计、有限取料/配置与保存核销，不构成持续来源或产量通过。R389原36,000-tick连续声明因33-tick间隙失败且未拼接；R438仍在运行，continuous credit为0、`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 完整29物料、连通氢、远端新Ti与本地新炼Ti、Warper与Rod各至少1/min并持续至少36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过；已通过的蓝图生命周期与Governor 2×范围保持原结论。封闭的H侧候选不重开。
- R438只按原声明收集；连续生产、来源条件及终端headroom仍须依据其完整终态和后续root核验，不以R440的取料或需求上限代替。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)、[R388 warper headroom](evidence/2026-10-09/warper-output-headroom-r388.md)、[R440需求与取料保存](evidence/2026-10-09/ti-warp-demand-save-r440.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
