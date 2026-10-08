# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R379–R388的warper仓库容量处理与R388独立核销见[warper成品腾挪与headroom](evidence/2026-10-09/warper-output-headroom-r388.md)。R377单次到料观察见[油路到料与窗口提前封闭](evidence/2026-10-09/oil-arrival-window-close-r377.md)；R323/R324持续供需来源条件仍未通过，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前R89，普通Save `102564883`/durable J102；external `3/20`、lifetime `378`。无在途或unknown；当前20写窗口仍开放但冻结，本次headroom片3个accepted额度已耗尽。
- R379读回时warper仓`5331`满载`3000`，有71个外排请求；生产端`5329`的r78进度为6,000,000，10个输出受满仓阻塞。R386随后普通转出item `1210×100`，Native仓存`3000→2900`、玩家库存`30→130`，并正常Save。取成品只腾出有限空间，不是产率或持续供给证据。
- R380有1笔已accepted的Move，随后Native终态为`action_failed/position_stalled`，停滞180 ticks；R382核销同一原动作并标记不重试该目标。R383完整库存未变、无在途，玩家距仓库73.1299m（在78m范围内），因此未再移动或重放。R384的7请求预检因caller误用5350而在0 accepted时停止；R387核实真实来向为`5365←5352`、Native路径202并修正固定参数。
- R388独立核对全部原计划、intent、唯一ACK与终态，及全库存/inc/held、7个受影响对象静态字段、完整Native路径202、供电、Journal和Save覆盖。没有新增建造、配置、手搓或升级。油路22 accepted修复预算已耗尽；R377的16/20窗口提前闭合后剩余4笔不结转。
- R377曾确认原油到达`707`并推进加工状态，但没有连续产率证明。执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。
- 执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R379的满仓堵塞读回与R386一次取货不提供warper持续产率；R377的一次原油到料读回也不构成油路持续供给通过。没有新增continuous credit。R323/R324来源条件失败及R344诊断保持不变；`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步由root另行确定连续供需窗口，按预声明范围fresh观察共享来源与完整清单；双自动补给、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
