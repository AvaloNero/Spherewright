# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R366中断后的原动作核销、正常保存和R372独立审计见[油路覆盖前缀与阶段核销](evidence/2026-10-08/oil-cover-prefix-r372.md)。此前R359/R361路线与材料资格见[油路绕行与材料封窗](evidence/2026-10-08/oil-detour-material-r365.md)。R323/R324持续供需来源条件仍未通过，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前R79，普通Save `102448091`/durable J102；external `12/20`、lifetime `371`。无在途或unknown，当前阶段冻结；20写窗口尚未闭合。
- R366接受了两笔建造：14条带，以及从source `6288`覆盖19条带。第二笔原Native动作超出caller等待时限后才到终态，原caller以exit 1结束；R368只读核销同一原动作，未重放。R369独立复核后才清除在途标记。R370正常Save `102448091`，R372独立核验两笔建造、保存覆盖、完整库存、相关静态连接、电力和Journal。合计33条新带已保存，未新建分拣器。
- 当前玩家库存为belt `2001×39`、basic sorter `2011×2`、fast sorter `2012×3`。结构修复预算只将R361的接受动作上限从21增加到22，以多留一笔正常Save；此前前缀6笔、R364的9笔、R366的2笔和R370的1笔保存共用18笔，剩余4笔为7条source-cover带、basic sorter、fast sorter及最终Save。结构上限仍为最多40条新belt和2个sorter；旧R355的19/20窗口已闭合，其余额不结转。
- R359的空路径段和两端cover预览仍只是分段资格，不代表整条路线已执行。剩余source端与旧消费者的接缝、实际到`707`的物流均待建成后fresh核验。
- 执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R366阶段中断已通过原动作终态、独立核销和正常Save收口；这不证明源端已接通或形成持续供给。R323/R324来源条件失败及R344诊断保持不变；没有新增continuous credit，`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步在新的有限施工范围打开后完成剩余四笔动作，并fresh核验两端接缝及实际到料；之后仍须单独完成持续供需和最终发行验收。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
