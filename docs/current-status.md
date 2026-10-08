# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R398当前资格核验与R407有限分拣器升级见[联合来源缺口与R391前缀核验](evidence/2026-10-09/joint-source-gap-and-prefix-r391.md)；R388仓储headroom见[warper成品腾挪与核销](evidence/2026-10-09/warper-output-headroom-r388.md)。R323/R324长窗来源条件失败仍在，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前普通Save `102710336`/revision `96`/durable J102；external `7/20`、lifetime `382`。无在途或unknown。R407的4笔accepted已由R409独立核销，正常保存覆盖，库存变化为2011 `+2`、2012 `−2`。
- R407将玩家正常移动到既有1233附近，并把既有1233、1223两只2011分拣器升为2012；根核对cargo、filter、互返连接、15项静态字段、本地电力、Journal与Save均通过。原生三格运输周期的progressRequired由600000降至300000，只有名义容量变化，尚未量测持续生产改善。原件`0506229be23b4192bbe9ac15ad393ee7:125` / SHA-256 `C077D4124B03A496B118B5C621607C51A2918692B8FCC030DF5ED861E62B74D5`；独立核验`e235841341a24b9d8bbdeb99623dd7f0:1` / SHA-256 `35C50AF0B193FBD106C7D136825CA7179E1BEB9B1E8605230E31E9C9592BD6F2`。
- R397精确核对发现，矿工86的resourceNode 203返回Native `INVALID_ENTITY`，仅该实体从`resourceNodeIds`移除；此结果按自然耗尽处理，其余16个源节点均有正证据。R398 whole-current资格核验已通过，并只允许这项精确基线校正；R395停止记录保留，不据此声称长期来源通过。R398当前cut读数为高能石墨1109=708、磁铁1102=6950、奇异物质1127=87、连通氢1120=136、钛锭1106=495；全局氢4845不等于连通氢，legacy4419已排除。
- R389的36,000-tick连续声明仍因sample间33-tick缺口在32,295 ticks后自动失败，未拼接或延长，continuous credit为0。R391前缀的warper、rod、Ti计数与库存差异仍只是诊断，不是产能/供给通过；source conditions与`wholeSupplyPassed`仍为false。
- R404仅因caller遗漏物理belt1222基线而在Native prepare前停止，0 accepted；R406核销并保留原once，随后按精确15项基线新声明，未重放。R392在首个Native请求前因函数引用错误停止，0写入；R394已完成16次只读（6.517秒、0 Game写），R398资格核验现已完成。执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`，已安装二进制仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R398当前资格核验和R407的名义运输周期提升均不构成持续来源/产量通过；R389连续观察仍失败，continuous credit为0、`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收。已通过的蓝图生命周期与Governor 2×范围保持原结论，不重开或扩大。
- 下一步root正设计H3345→3074及3074→3073两条并行普通分拣器；尚未批准施工或供需通过。既有3083已接入3073，不重复铺设；石墨5196/5581已有名义能力，不盲目升级。之后仍需重新预声明并通过连续供需窗口、双自动补给、整合保存恢复及最终同SHA双候选包。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)、[R388 warper headroom](evidence/2026-10-09/warper-output-headroom-r388.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
