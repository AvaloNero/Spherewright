# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。最新阶段保存与独立审计见[R334铁入口升级事件](evidence/2026-10-08/iron-admission-upgrade-r334.md)；联合供需长窗的来源条件未通过，见[R323/R324原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前为R47/J102，普通Save `101943803`；external `11`、lifetime `351`。固定20写窗口仍开放但冻结，无在途或unknown。R334独立审计确认本阶段已保存的业务动作均已核销。
- R327有一次成功的物料转移；客户端随后因库存汇总解析失败停止。R331/R333只读核销了同一原动作的Native终态与双边`1203`守恒，没有重放或重复转移。
- 后续三笔accepted为r88×1手搓、原生升级与普通保存。相对转移核销后的库存净变化为`1203 −1`、`2011 −1`、`2012 +1`；玩家最终持有`2011×1`、`2012×1`、`1203×0`，出发套件其余项目不变。
- 物件`2351`升级后保持原ID，现为`2012/filter1101`，接入`2348→723`。原货物数量及增量、两条互惠连接和分拣器循环比例保留；9个相关DTO静态检查及本地满电检查通过。
- 执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R334证明本阶段保存与独立核销完成，不证明持续产量或整体供给。R323的`36,316`连续tick观察仍由R324判定`sourceConditionsPassed=false`，continuous credit为`0`，整案保持`executable=false`、`wholeSupplyPassed=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步只读检查铁入口升级效果及旧油/石墨共享支路的真实供需，再据此确定有界修复和新长窗；R323/R324未通过结论保持不变。

历史证据只通过[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R282铁路由封窗](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)、[R303远征ILS封窗](evidence/2026-10-08/departure-ils-ten-r303.md)、[R312小型套件封窗](evidence/2026-10-08/departure-small-kit-ten-r312.md)、[R320 Rod carry阶段](evidence/2026-10-08/departure-rod-carry-seven-r320.md)、[R321/R322观察缺口](evidence/2026-10-08/joint-observation-gap-r321.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)查阅；本快照不复述旧阶段流水。
