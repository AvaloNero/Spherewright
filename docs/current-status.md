# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。当前状态为R449核验后的冻结截面；石料矿源仍未恢复，见[石料来源恢复阶段](evidence/2026-10-09/stone-source-recovery-r449.md)。R438/R441长窗和R443清单仍见[连续来源书挡与出发清单](evidence/2026-10-09/full-source-bookends-r441.md)；R323/R324来源条件失败仍保留，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- R447正常保存`102844177`/R112/J102；external `17/20`、lifetime `392`，R449独立核验后无在途或unknown，普通写入冻结；当前窗口尚未闭合。
- R445/R446核实已有15台矿机均未覆盖Stone矿点；完整116个Stone矿点读回中相关矿机计数为0。1005是石料。R447只回收了已耗尽且空载的矿机86并保存；新矿机候选在对象5318处被Native碰撞拒绝，未提交建造。详见[石料来源恢复阶段](evidence/2026-10-09/stone-source-recovery-r449.md)。
- R449独立核销通过的是回收与保存事实，不代表石料补源或连续供料通过；未决清零，窗口当前为17/20且尚未闭合。下一步先完成该窗口封存，再依据5318碰撞与当前接口能力选择可执行的有界不同布局；若需额外代码，须另做相关测试并经同批实机核验。尚未批准新施工。

## Gate 2 边界

- R441虽核实原36090-tick采样无缺口，但1109高能石墨736→665（容差3）、1210 Warper3071→3032（容差1）、1102磁铁6869→6845（容差3）、1005石料2855→2743（容差8）、1127奇异物质127→94（容差1）均超出允许下降。R389原36,000-tick连续声明因33-tick间隙失败且未拼接；本轮来源条件未通过，continuous credit为0、`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 完整29物料、连通氢、远端新Ti与本地新炼Ti、Warper与Rod各至少1/min并持续至少36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过；已通过的蓝图生命周期与Governor 2×范围保持原结论。封闭的H侧候选不重开。
- R449仅核销空载耗尽矿机的回收与保存；石料补源和接线尚未施工。连续来源、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)、[R388 warper headroom](evidence/2026-10-09/warper-output-headroom-r388.md)、[R440需求与取料保存](evidence/2026-10-09/ti-warp-demand-save-r440.md)、[R441连续窗口与清单](evidence/2026-10-09/full-source-bookends-r441.md)、[R449石料来源恢复](evidence/2026-10-09/stone-source-recovery-r449.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
