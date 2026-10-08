# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R377核对完整油路与707实际到料见[油路到料与窗口提前封闭](evidence/2026-10-09/oil-arrival-window-close-r377.md)。R373施工与R375结构核验见[油路路线完成](evidence/2026-10-09/oil-route-completion-r375.md)；R323/R324持续供需来源条件仍未通过，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前R86，普通Save `102506542`/durable J102；external `16/20`、lifetime `375`。无在途或unknown。该20写窗口在16笔处提前封闭，未用4笔不结转；lifetime未清零，新窗口未开。
- R377核销原16笔unique Native动作及Save覆盖。R373的最后4笔完成40条新belt与两个filter `1007` 的sorter：`6316`连接旧`6063`与新头`6275`，`6315`连接新尾`6314`与旧消费者`2804`。Root核验完整有向路径`5949→6063→6316→新40带→6315→2804→707`，完整快照6316 built/0 prebuild；相对6274基线新增42个Native对象、0删除、0不互惠边，旧端口变化仅为声明的物理接入。
- R373/R375先前读回tick `102506582`时，707原油输入/精炼油输出/进度均为0；R376/R377随后同tick读回tick `102531072`为原油输入2、`working=true`、进度270000、精炼油输出1、power 1。这证明原油到达707并推进一次加工状态，不证明持续产率。91个同tick物料对象与3项电力读回按各自采样边界核验。R377按原16笔动作核销口径确认全库存净差：belt `2001 +2`、basic sorter `2011 0`、fast sorter `2012 +1`、circuit `1301 −3`。
- R361结构上限40条新belt/2个sorter与22笔accepted施工上限均已用尽；玩家当前stock清单以R377完整背包读回为准，不将上述净差当作当前库存。R366曾发生caller等待超时，已由R368只读核销同一原动作，未重放。
- 执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R377确认一笔原油到达`707`并出现加工进度，但没有连续产率证据或新增continuous credit。R323/R324来源条件失败及R344诊断保持不变；`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步由root另行确定持续供需窗口，按原范围fresh观察共享来源和完整清单；最终双自动补给、连续至少36,000 ticks、整合保存恢复及同SHA双候选包仍未验收。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
