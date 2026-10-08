# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R438 长窗已结束，R441 独立核验确认连续覆盖但来源库存条件未通过；R443 准备清单补充见[连续来源书挡与出发清单](evidence/2026-10-09/full-source-bookends-r441.md)。较早钛需求修正见[R423证据](evidence/2026-10-09/minimum-ti-demand-r423.md)，R323/R324来源条件失败仍保留，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前为健康的同一 owned world；R440正常保存`102781516`/R109/J102，external `15/20`、lifetime `390`，无在途或unknown，普通写入冻结。
- R438封存观察108个样本、覆盖36090个连续game ticks且无缺口；R441用原声明逐项核验，未改变采样边界。四项计数下界达到目标：1210≥4.5/min、1802≥1.6/min、P102钛矿1004≥36.3/min、P104钛锭1106≥13.1/min；三厂样本供电均满服务，连通氢净增4。R441的物料库存核对仍有五项超容差下降，故来源条件未通过，详见[本事件](evidence/2026-10-09/full-source-bookends-r441.md)。
- R443核对已保存Native玩家库存满足有限出发清单：ILS2104×1、运输船5002×1、矿机2301×1、电塔2201×2、风机2203×2、传送带2001×30、分拣器2011×2、Warper1210×30、Rod1802×5。此清单是有限准备物资；运输船为既有库存，清单与有限手搓/取料不证明持续供给或新船生产。详见[准备清单补充证据](evidence/2026-10-09/full-source-bookends-r441.md)。

## Gate 2 边界

- R441虽核实原36090-tick采样无缺口，但1109高能石墨736→665（容差3）、1210 Warper3071→3032（容差1）、1102磁铁6869→6845（容差3）、1005铁矿2855→2743（容差8）、1127奇异物质127→94（容差1）均超出允许下降。R389原36,000-tick连续声明因33-tick间隙失败且未拼接；本轮来源条件未通过，continuous credit为0、`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 完整29物料、连通氢、远端新Ti与本地新炼Ti、Warper与Rod各至少1/min并持续至少36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过；已通过的蓝图生命周期与Governor 2×范围保持原结论。封闭的H侧候选不重开。
- 下一步由root基于同一封存观察诊断五项库存下降并选择最小修复或新预声明实验；本记录未批准新施工。Warper与Rod持续来源、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)、[R388 warper headroom](evidence/2026-10-09/warper-output-headroom-r388.md)、[R440需求与取料保存](evidence/2026-10-09/ti-warp-demand-save-r440.md)、[R441连续窗口与清单](evidence/2026-10-09/full-source-bookends-r441.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
