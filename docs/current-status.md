# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。当前代码新增空带头部有限回收适配，但仅离线实现、测试和构建完成；尚未同批冷部署或实机回收，详见[空带回收适配](evidence/2026-10-09/empty-belt-recovery-adapter-r454.md)。R451封窗与石料节点核验见[石料来源阶段](evidence/2026-10-09/stone-source-recovery-r449.md)；R438/R441连续来源失败仍保留，见[来源书挡与清单](evidence/2026-10-09/full-source-bookends-r441.md)。

## 当前 owned 状态

- live world仍为R112 / Save `102844177` / J102，external `17/20`、lifetime `392`；R451已核销原17笔并完整封窗，剩余3不转移，新20窗口未开，无在途或unknown，普通写入冻结。
- R453 root独立审计确认旧占位yaw345→5318与yaw135→3725两次碰撞拒绝，关闭该候选族，不再旋转微移；保留3725现有活线。1005为石料；现有15台矿机未覆盖完整116个Stone矿点。详见[石料来源阶段](evidence/2026-10-09/stone-source-recovery-r449.md)。
- R454仅增加针对完全隔离、空载、普通2001带链头的正常回收适配：最多16带/512 cells，按fresh完整路径货物、几何、身份、入边引用检查后调用一次原生DoDismantleObject，并核对精确退款与剩余链；没有直接改写游戏数组或自动重建。相关Core 118项、MCP 3项及完整Plugin Release构建已通过，使用Native DLL哈希`6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`。当前installed cohort仍是`863d35546f6cb49fcdaec5b5814869d1e13af42b`，新适配尚未冷部署或实机验证。
- 下一步是同批冷部署后有限正常回收5325，再依序核验5324至5316；后续石料来源与重新接线仍需fresh Native资格。以上离线代码验证不等于实机回收、来源恢复或持续供料通过。

## Gate 2 边界

- R441虽核实原36090-tick采样无缺口，但1109高能石墨736→665（容差3）、1210 Warper3071→3032（容差1）、1102磁铁6869→6845（容差3）、1005石料2855→2743（容差8）、1127奇异物质127→94（容差1）均超出允许下降。R389原36,000-tick连续声明因33-tick间隙失败且未拼接；本轮来源条件未通过，continuous credit为0、`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 完整29物料、连通氢、远端新Ti与本地新炼Ti、Warper与Rod各至少1/min并持续至少36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过；已通过的蓝图生命周期与Governor 2×范围保持原结论。封闭的H侧候选不重开。
- R454实现和离线测试不改变R438/R441来源条件失败；continuous credit仍为0，`wholeSupplyPassed=false`。石料补源、接线、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)、[R388 warper headroom](evidence/2026-10-09/warper-output-headroom-r388.md)、[R440需求与取料保存](evidence/2026-10-09/ti-warp-demand-save-r440.md)、[R441连续窗口与清单](evidence/2026-10-09/full-source-bookends-r441.md)、[R449石料来源与封窗](evidence/2026-10-09/stone-source-recovery-r449.md)、[R454空带回收适配](evidence/2026-10-09/empty-belt-recovery-adapter-r454.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
