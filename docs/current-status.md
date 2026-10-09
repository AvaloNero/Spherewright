# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前执行安装批次仍来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508同SHA双包仅离线预检通过，不是实际Mod Manager安装、实机验收或最终发行批准。

## 当前 owned 状态

- 当前为owned P104/R26；最后一次正常主档Save tick `105130853`，此前Journal为J102。固定20窗口external 8/20、lifetime 416，opening lifetime 408；四笔材料动作已由该Save覆盖，四笔accepted施工尚未覆盖。R577之后没有fresh closing Journal、全电网或prebuild完整页，不能将此前J102和旧电力观察算作本次closing证据。
- R577完成高端双接缝Native cover：由6332经16条NEW带材接至6318，背包带材163→147。root核验57成员的完整开放空载Native路径和94对象受影响切面；旧静态/互惠连接保全，三架无人机闲置。该施工尚未被Save `105130853`覆盖。
- R574的Move在Native prepare正例后，调用者因读取不存在的本地`plannedPosition`字段停止；没有commit或accepted，旧幂等键行保留。它是调用方解析错误，不是Native拒绝。根修正后，R579已获准在同一scope内续做一次Move和5段北接，当前有限调用仍在执行；不预写其结果或耗料。
- 石料整案累计消耗为17笔accepted、53条带材、6个跨度和2次移动。外层scope上限16笔/1100请求/2400秒，现已用4笔/402请求/1166980.7743毫秒，余12笔/698请求/1233秒；当前routing子片上限8笔/620请求/1500秒，现已用2笔/194请求/849726.5473毫秒，余6笔/426请求/650秒。R557已核准routing子片跨度上限由10调整为11；整案上限200条带材/30笔accepted/5次Save不变。不补额度、不重做成功前缀。
- 矿机5325仍为network 0、Native服务比率不可用（`null`）、缓冲为空；来源未接电或开采，`sourceAdmitted=false`、`wholeSupplyPassed=false`、continuous credit 0。过滤、供电及唯一末尾Save仍待验证。
- 29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收仍未通过。历史R498 `outcome_unknown`保持原判定且不重放；已通过的蓝图生命周期和Governor 2×范围保持。

阶段索引：[R577高端双接缝Native核验](evidence/2026-10-10/stone-raised-dual-cover-r577.md)、[R573上坡动作核销与余段预算](evidence/2026-10-10/stone-uphill-prefix-r573.md)、[R568下坡接入与关键接口复核](evidence/2026-10-10/stone-overpass-critical-interface-r568.md)、[R546石料有限备料与保存核销](evidence/2026-10-10/stone-finite-material-kit-r546.md)、[R536石料矿机出口与封窗核销](evidence/2026-10-10/stone-native-source-outlet-r536.md)、[R528石料来源接近资格](evidence/2026-10-10/stone-source-approach-r528.md)、[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
