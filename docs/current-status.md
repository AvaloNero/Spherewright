# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前安装批次来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508对同一源码SHA的双包离线预检通过，但不是实际Mod Manager安装、游戏运行验收或最终发行批准。

## 当前 owned 状态

- R546保存后当前为和平、非沙盒、1×的owned P104/R18健康状态；正常主档Save tick `105130853`，Journal完整且durable 102。新固定20窗口external 4/20、lifetime 412，四笔原动作均已由Save覆盖；当前无在途或未决动作，阶段末冻结。前一R536窗口独立封闭，累计计数未重置；历史R498的`outcome_unknown`仍保持原判定。
- R544完成一次正常Move、从1511取铁块1101×165、按r84×55递归制作165条带材并正常保存；原生直接账单为铁块1101×110、齿轮1201×55→带材2001×165，实际净耗铁块165、齿轮为递归中间产物。背包带材31→196，铁块与齿轮归零，其他count/inc/held及位置保持。R541成功Move前缀与R544续接合计294/800请求、407.724/1200秒、4/30 accepted。
- R541的caller在材料prepare前因本地Hashtable的`Count`键遮蔽而停止；该次Move已accepted并成功。R543只读核销原Move，未重放；其后继续剩余阶段。该caller失败不代表Native拒绝。
- 工厂完整快照仍为6317 built/0 prebuild；R546无新增或移除对象，旧静态及互惠拓扑保持。矿机5325仍network 0、Native服务比率不可用（`null`），出口86→6317空载；本阶段没有接线、开采或新增供电。R538/R540的来源与风机点位是资格结果，不证明产量或供电已建立。
- R497总预算当前累计为13/30 accepted、4/200条带、1/2个分拣器、1/1台矿机、2/4次移动、2/4份材料处理、4/5次保存、2/10段带材；风机与杆仍为0。下一已授权有限工作是先Native核验空接收端向来源延伸，再按预算处理运输、过滤连接与供电；具体动作遵守逐阶段caller边界。
- 持续来源条件仍未通过：`wholeSupplyPassed=false`、continuous credit为0。29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收均未通过。已通过的蓝图生命周期和Governor 2×范围保持，已封闭的H侧候选不重开。

阶段索引：[R546石料有限备料与保存核销](evidence/2026-10-10/stone-finite-material-kit-r546.md)、[R536石料矿机出口与封窗核销](evidence/2026-10-10/stone-native-source-outlet-r536.md)、[R528石料来源接近资格](evidence/2026-10-10/stone-source-approach-r528.md)、[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
