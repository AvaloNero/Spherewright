# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R373绕行施工完成与R375独立核验见[油路路线完成与供给边界](evidence/2026-10-09/oil-route-completion-r375.md)。此前中断动作核销见[R372阶段核销](evidence/2026-10-08/oil-cover-prefix-r372.md)；R323/R324持续供需来源条件仍未通过，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前R86，普通Save `102506542`/durable J102；external `16/20`、lifetime `375`。无在途或unknown，阶段冻结；当前20写窗口尚未闭合。
- R373的4笔accepted包含最后7条source-cover带、两个带filter `1007`的分拣器及正常Save，101请求/127.70秒。sorter `6315`（`2011`）接`6314`至旧消费者`2804`；sorter `6316`（`2012`）将旧`6063`接至新头`6275`，由此完成source侧接入。R375核验完整40条新带、39条互惠连接、实际Native带点、旧16个对象与既有前缀的静态状态，以及新增两个分拣器的本地满供；所有施工均由Save `102506542`覆盖。
- 当前玩家库存为belt `2001×32`、basic sorter `2011×1`、fast sorter `2012×2`。R361施工接受上限经R372只为正常保存由21增至22；前缀6笔、R364的9笔、R366的2笔、R370的Save及R373的4笔共22笔已用尽。结构上限为最多40条新belt和2个sorter，现已达到；旧R355的19/20窗口已闭合且不结转余额。
- R366第二笔原动作曾超出caller等待时限；R368只读核销同一终态，未重放，R369独立复核后清除在途标记。R372后33条前缀先经Save覆盖，本阶段追加7条带及两个sorter。R366中断不作为Native施工拒绝。
- 路线已按限定数量建成并由保存覆盖，但实际到下游`707`的物流尚待观察；建造终态和本地满供不构成源供给或持续生产证据。
- 执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R373施工及R375独立核验通过，不代表来源实际向`707`供货或形成持续供给。R323/R324来源条件失败及R344诊断保持不变；没有新增continuous credit，`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步fresh核验已建路线的完整工厂状态与实际到`707`供料，再单独完成持续供需和最终发行验收；当前accepted施工上限已耗尽。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
