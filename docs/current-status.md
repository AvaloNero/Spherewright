# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前执行安装批次仍来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508同SHA双包仅离线预检通过，不是实际Mod Manager安装、实机验收或最终发行批准。

## 当前 owned 状态

- 当前为和平、非沙盒、1×的owned P104/R24健康状态，正常主档Save tick `105130853`，Journal完整且durable 102。固定20窗口external 7/20、lifetime 415，opening lifetime 408；无在途或unknown，写入冻结。四笔材料动作已由该Save覆盖，三笔已accepted的施工动作尚未被后续Save覆盖。
- R569上坡施工原调用者在420秒预算耗尽后停止；R572只读核实同一原动作Native终态为completed/succeeded，R573独立审计核对动作与现场。上坡新增20条带材，背包带材183→163；无人机均空闲且无预建筑。调用者超时不代表Native失败，没有重放。较早R558与R564的核销和接口状态见[R568事件](evidence/2026-10-10/stone-overpass-critical-interface-r568.md)。
- 原方案总预算为16笔accepted、37条带材、5个跨度和2次移动。所属scope为16笔/1100请求/2400秒，目前3笔/307请求/742438.8117毫秒，余13笔/793请求/1657秒；当前routing子片为8笔/620请求/1500秒，目前1笔/99请求/425184.5847毫秒，余7笔/521请求/1074秒。上坡虽已建成，但高端Native双接缝、五段北接、移动、过滤分拣器、2风机、至多2电塔及唯一末尾Save仍待核验。
- 矿机5325仍为network 0、Native服务比率不可用（`null`）、缓冲为空。当前未证明源已接电、开采或开始供料；整体仍为`sourceAdmitted=false`、`wholeSupplyPassed=false`、continuous credit 0。
- 29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收仍未通过。历史R498 `outcome_unknown`保持原判定且不重放；已通过的蓝图生命周期和Governor 2×范围保持。

阶段索引：[R573上坡动作核销与余段预算](evidence/2026-10-10/stone-uphill-prefix-r573.md)、[R568下坡接入与关键接口复核](evidence/2026-10-10/stone-overpass-critical-interface-r568.md)、[R546石料有限备料与保存核销](evidence/2026-10-10/stone-finite-material-kit-r546.md)、[R536石料矿机出口与封窗核销](evidence/2026-10-10/stone-native-source-outlet-r536.md)、[R528石料来源接近资格](evidence/2026-10-10/stone-source-approach-r528.md)、[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
