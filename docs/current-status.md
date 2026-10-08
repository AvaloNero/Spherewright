# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。R364材料阶段与R365独立核销、R359绕行接口资格及R361有界施工预算见[油路绕行与材料封窗](evidence/2026-10-08/oil-detour-material-r365.md)。R323/R324的持续供需来源条件仍未通过，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前R74，普通Save `102313600`/durable J102；新窗口external `9/20`、lifetime `368`。无在途或unknown，本阶段已冻结。R355的旧20写窗口在19笔处封闭，未用槽位不结转。
- R364的9笔accepted由Save `102313600`覆盖：移动、从仓45取Fe×28、r5×14、r84×14、从727取Motor×1、r85×3、r88×1、返程移动与保存。114请求/66.55秒；R365逐项核对了Native终态、库存与增量、两仓扣款、静态连接/电力及保存。
- 相对阶段起点，材料净变化为belt `2001 +42`、basic sorter `2011 +1`、fast sorter `2012 +2`、circuit `1301 −3`；当前库存分别为belt `72`、basic sorter `2`、fast sorter `3`。本阶段未建造工厂对象。
- R359只读资格验证了东/南地面绕行的三个空路径段，以及源端/目的端各自的非拆除式cover接口；单端span预览不等于整条路线可执行。R361把该结构修复全阶段上限定为21笔accepted（此前前缀6笔、R364 9笔、余6笔），范围上限是新建belt `40`条、sorter `2`个，剩余动作为3条belt、2个sorter和Save。此施工尚未执行，是下一步获批的有限动作。
- 执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- 材料准备、动作核销与路线分段资格不构成油路施工或持续供给通过。R323/R324来源条件未通过结论和R344诊断保持不变；无新增continuous credit，`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步按R361限定的东/南结构绕行执行获批的有限施工，并在实际建成后fresh验证未来带点、源端接缝及到下游`707`的真实物流；新窗口仅按既有授权和上限执行。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
