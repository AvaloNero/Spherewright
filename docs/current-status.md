# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R389/R391联合来源差异与前缀诊断见[联合来源缺口与R391前缀核验](evidence/2026-10-09/joint-source-gap-and-prefix-r391.md)。R388仓储headroom阶段见[warper成品腾挪与核销](evidence/2026-10-09/warper-output-headroom-r388.md)；R323/R324的来源条件失败仍在，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前R89，普通Save `102564883`/durable J102；external `3/20`、lifetime `378`。无在途或unknown，当前写窗口开放但冻结。R389/R391均为只读核验，没有新accepted。
- R389原声明尝试36,000-tick连续观察，829个请求/607.406秒/0 accepted；sample63仅覆盖单段连续32,295 ticks，sample64开始前有33 ticks缺口。原声明自动失败并停止，不拼接、不延长，continuous credit仍为0。
- R391核对17组完整原始cuts、29种物理库存以及远端source169、本地Ti254和9条路径、16个燃料发电机、两座实验室、三厂电力。末组只是scheduled sample，没有closing cut或closing nodes，不能当成全窗库存通过。prefix诊断超出容差：石墨1109为761→732（−29，容差3），氢1102为7009→6986（−23，容差3），材料1127为131→101（−30，容差1），连通氢净差−2。
- 同一32,295-tick前缀的Native计数为warper `1210 [39,40]`、rod `1802 [16,16]`、P102钛矿石`1004 [0,0]`、P104钛锭`1106 [24,47]`，只作诊断；warper/rod的writer下界4.1/min与1.6/min不计为通过。钛源库存382→382，本地钛矿石3662→3582、钛锭3713→3745；这些差值不证明矿竭或唯一根因。
- 来源条件未通过，`wholeSupplyPassed=false`；R323/R324来源条件失败和R344诊断保持不变。R392有界只读因果诊断已获准并正在执行，结果未包含在本快照。R388仓储headroom与R377单次油到料都不是持续产率通过。执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R389连续观察缺口和R391的超容差库存净降均不证明唯一根因或持续来源通过；R379/R386的一次warper取货及R377的一次油到料也不构成连续产率。没有新增continuous credit；`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步先完成R392已授权的有界只读因果诊断，再针对已定位的来源/分流/堵塞选择最小修复；其后重新预声明并执行连续供需窗口。双自动补给、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)、[R388 warper headroom](evidence/2026-10-09/warper-output-headroom-r388.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
