# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前安装批次仍来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508对同一源码SHA的双包离线预检通过，但不是实际Mod Manager安装、游戏运行验收或最终发行批准。

## 当前 owned 状态

- R519封存后，当前为和平、非沙盒、1×的owned P104/R4健康状态；正常主档保存tick `105012378`，Journal完整且durable 102。external计数4/20、lifetime 403；当前无在途或未决动作，写入冻结，窗口未关闭也未新开。历史R498的 `outcome_unknown` 仍保留，不改判成功。
- R515有限阶段新增普通分拣器5324，filter `1005`，接通 `5316→87`。Native终态与读回通过；正常Save覆盖两笔写入。全厂读回6314 built/0 prebuild；对照基线只新增5324与5316/87间的精确连接，其他静态、拓扑、互惠边均未变。5条完整Native货物路径及153格空载路径的成员、几何和拓扑保持。
- 玩家位置与全背包count/inc/held保持；本次唯一材料净差为2011分拣器−1，当前2001×33、2011×3。新增消费者的电网点读满供。
- 持续来源条件仍未通过：`wholeSupplyPassed=false`、continuous credit为0。29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收均未通过。既有蓝图生命周期和Governor 2×范围保持，已封闭的H侧候选不重开。
- 下一候选是在不同的group 19矿点277做fresh Native矿机选择器只读资格并正常保存；尚未批准执行，不包括新建矿源或持续供给结论。

阶段索引：[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
