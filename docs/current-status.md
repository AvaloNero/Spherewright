# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前安装批次来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508对同一源码SHA的双包离线预检通过，但不是实际Mod Manager安装、游戏运行验收或最终发行批准。

## 当前 owned 状态

- R526普通保存后当前为和平、非沙盒、1×的owned P104/R7健康状态；Save tick `105072201`，Journal完整且durable 102。external计数6/20、lifetime 405；无在途或未决动作，写入冻结，窗口未关闭也未新开。历史R498的 `outcome_unknown` 仍保留，不改判成功。
- R520的Move在固定30请求预算结束前已accepted；R524仅查询同一原action并核实Native终态成功，R525独立核销，无重放。玩家已正常落地，累计1549个Native ticks；R526用一笔普通Save覆盖此前Move。
- R515新增的普通分拣器5324使用filter `1005`，连接 `5316→87`，由R519独立核销并正常保存。R519完整工厂读回为6314 built/0 prebuild；仅新增该分拣器及其接缝，5条完整Native货物路径与153格空载路径的几何和拓扑保持。玩家位置与背包count/inc/held未变，本次阶段未建造、未开采。
- R528对group 19矿点277的矿机候选 `2301×1`及节点262、264、267、271、273、277作了Native正向资格核验，节点均显示正余量；没有提交建造或开采。最近一次完整工厂捕获仍是R519的6314 built/0 prebuild，不能视为本次新捕获。
- 持续来源条件仍未通过：`wholeSupplyPassed=false`、continuous credit为0。29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收均未通过。已通过的蓝图生命周期和Governor 2×范围保持；已封闭的H侧候选不重开。

阶段索引：[R528石料来源接近资格](evidence/2026-10-10/stone-source-approach-r528.md)、[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
